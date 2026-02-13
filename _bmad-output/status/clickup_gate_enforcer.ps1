param(
  [switch]$Apply = $true,
  [switch]$IncludeSystemTasks = $false,
  [string]$SpaceId = "",
  [string]$DotEnvPath = ".env"
)

$ErrorActionPreference = 'Stop'

# Repository policy: ClickUp actions are hard-disabled for PDLC workflows.
Write-Output 'ClickUp integrations are disabled by repository policy. Gate enforcer exited without changes.'
exit 0

function Parse-DotEnvValue {
  param([string]$Raw)
  $v = $Raw.Trim()
  if ($v.StartsWith('"') -and $v.EndsWith('"')) { return $v.Substring(1, $v.Length - 2) }
  return $v
}

function Load-DotEnv {
  param([string]$Path)
  $map = @{}
  if (!(Test-Path $Path)) { throw "Missing dotenv: $Path" }
  Get-Content $Path | ForEach-Object {
    $line = $_.Trim()
    if (!$line -or $line.StartsWith('#')) { return }
    $idx = $line.IndexOf('=')
    if ($idx -lt 1) { return }
    $k = $line.Substring(0, $idx).Trim()
    $map[$k] = Parse-DotEnvValue -Raw ($line.Substring($idx + 1))
  }
  return $map
}

function Is-Empty {
  param($Value)
  if ($null -eq $Value) { return $true }
  $s = "$Value".Trim()
  return [string]::IsNullOrWhiteSpace($s)
}

function Normalize-String {
  param($Value)
  if ($null -eq $Value) { return '' }
  return ("$Value").Trim().ToLowerInvariant()
}

function Convert-CustomFieldValue {
  param($Field)
  if ($null -eq $Field.value) { return $null }

  switch -Regex ($Field.type) {
    '^checkbox$' {
      return [bool]$Field.value
    }
    '^number$' {
      try { return [double]$Field.value } catch { return $Field.value }
    }
    '^drop_down$' {
      $raw = $Field.value
      if ($Field.type_config -and $Field.type_config.options) {
        foreach ($opt in @($Field.type_config.options)) {
          if ($opt.id -eq $raw) { return $opt.name }
          if ("$($opt.orderindex)" -eq "$raw") { return $opt.name }
          if ($opt.name -eq $raw) { return $opt.name }
        }
      }
      return $raw
    }
    default {
      return "$($Field.value)"
    }
  }
}

function Build-FieldMap {
  param($Task)
  $map = @{}
  foreach ($cf in @($Task.custom_fields)) {
    $map[$cf.name] = Convert-CustomFieldValue -Field $cf
  }
  return $map
}

function Validate-Gates {
  param($Status, $Fields)

  $statusNorm = Normalize-String $Status
  $issues = New-Object System.Collections.Generic.List[string]
  $fallback = $null
  $gate = $null

  if ($statusNorm -eq 'definition') {
    $gate = 'G1 Discovery->Definition'
    $fallback = 'Discovery'

    if (Is-Empty $Fields['Strategic_Doc_Link']) { $issues.Add('Strategic_Doc_Link missing') }

    $evidence = Normalize-String $Fields['Evidence_Level']
    if ($evidence -eq '' -or $evidence -eq 'low') { $issues.Add('Evidence_Level must be Medium or High') }

    $interviews = 0
    try { $interviews = [int]$Fields['Interview_Count'] } catch { $interviews = 0 }
    if ($interviews -lt 5) { $issues.Add('Interview_Count must be >= 5') }
  }

  if ($statusNorm -eq 'architecture review') {
    $gate = 'G2 Design->Architecture Review'
    $fallback = 'Design'

    if (Is-Empty $Fields['Figma_File_Link']) { $issues.Add('Figma_File_Link missing') }
    if (Is-Empty $Fields['Figma_Flow_Link']) { $issues.Add('Figma_Flow_Link missing') }
    if (Is-Empty $Fields['Figma_Frame_IDs']) { $issues.Add('Figma_Frame_IDs missing') }

    $ux = Normalize-String $Fields['UX_Maturity']
    if ($ux -ne 'build-approved') { $issues.Add('UX_Maturity must be Build-approved') }

    if ($Fields['Edge_States_Documented'] -ne $true) { $issues.Add('Edge_States_Documented must be true') }
    if ($Fields['Accessibility_Checked'] -ne $true) { $issues.Add('Accessibility_Checked must be true') }
  }

  if ($statusNorm -eq 'ready for build') {
    $gate = 'G3 Architecture Review->Ready for Build'
    $fallback = 'Architecture Review'

    if (Is-Empty $Fields['Strategic_Doc_Link']) { $issues.Add('Strategic_Doc_Link missing') }
    if (Is-Empty $Fields['Economic_Hypothesis']) { $issues.Add('Economic_Hypothesis missing') }

    if ($Fields['Architecture_Approved'] -ne $true) { $issues.Add('Architecture_Approved must be true') }
    if ($Fields['Security_Approved'] -ne $true) { $issues.Add('Security_Approved must be true') }
    if ($Fields['Data_Compliance_Approved'] -ne $true) { $issues.Add('Data_Compliance_Approved must be true') }
    if ($Fields['NFR_Defined'] -ne $true) { $issues.Add('NFR_Defined must be true') }
    if ($Fields['Dependencies_Mapped'] -ne $true) { $issues.Add('Dependencies_Mapped must be true') }
    if ($Fields['Acceptance_Criteria_Defined'] -ne $true) { $issues.Add('Acceptance_Criteria_Defined must be true') }
    if ($Fields['Dev_Handoff_Confirmed'] -ne $true) { $issues.Add('Dev_Handoff_Confirmed must be true') }
  }

  if ($statusNorm -eq 'released') {
    $gate = 'G4 Ready for Release->Released'
    $fallback = 'Ready for Release'

    if (Is-Empty $Fields['Release_Metric']) { $issues.Add('Release_Metric missing') }

    if ($null -eq $Fields['Baseline_Value']) {
      $issues.Add('Baseline_Value missing')
    }

    if (Is-Empty $Fields['Metrics_Doc_Link']) { $issues.Add('Metrics_Doc_Link missing') }
  }

  [pscustomobject]@{
    gate = $gate
    fallback = $fallback
    issues = @($issues)
    valid = (@($issues).Count -eq 0)
  }
}

$envMap = Load-DotEnv -Path $DotEnvPath
$clickupEnabledRaw = 'false'
if ($envMap.ContainsKey('CLICKUP_INTEGRATIONS_ENABLED')) {
  $clickupEnabledRaw = $envMap['CLICKUP_INTEGRATIONS_ENABLED']
} elseif ($envMap.ContainsKey('CLICKUP_ENABLED')) {
  $clickupEnabledRaw = $envMap['CLICKUP_ENABLED']
}

if (Is-Empty $clickupEnabledRaw) { $clickupEnabledRaw = 'false' }
$flag = Normalize-String $clickupEnabledRaw
if (!(@('1', 'true', 'yes', 'on', 'enabled').Contains($flag))) {
  Write-Output 'ClickUp integrations are disabled. Gate enforcer exited without changes.'
  exit 0
}

$token = $envMap['CLICKUP_TOKEN']
if (!$token) { throw 'CLICKUP_TOKEN missing in .env' }
if ([string]::IsNullOrWhiteSpace($SpaceId)) { $SpaceId = $envMap['CLICKUP_SPACE_ID'] }
if ([string]::IsNullOrWhiteSpace($SpaceId)) { throw 'CLICKUP_SPACE_ID missing in .env and no -SpaceId provided' }

$headers = @{ Authorization = $token; 'Content-Type' = 'application/json' }
$folderResp = Invoke-RestMethod -Method Get -Uri "https://api.clickup.com/api/v2/space/$SpaceId/folder?archived=false" -Headers $headers

$lists = @()
foreach ($folder in @($folderResp.folders)) {
  foreach ($list in @($folder.lists | Where-Object { $_.name -like '*- Control' })) {
    $lists += [pscustomobject]@{ folder = $folder.name; list = $list.name; list_id = $list.id }
  }
}

$results = New-Object System.Collections.Generic.List[object]
$scanned = 0
$evaluated = 0
$corrected = 0
$errorCount = 0

foreach ($l in $lists) {
  $taskResp = Invoke-RestMethod -Method Get -Uri "https://api.clickup.com/api/v2/list/$($l.list_id)/task?archived=false&include_closed=true&page=0&subtasks=true" -Headers $headers
  foreach ($task in @($taskResp.tasks)) {
    $scanned += 1

    $name = "$($task.name)"
    if (!$IncludeSystemTasks) {
      if ($name -like 'PDLC *' -or $name -like 'Blocked:*' -or $name -like 'GateTest *') {
        continue
      }
    }

    $status = "$($task.status.status)"
    $statusType = "$($task.status.type)"
    if ((Normalize-String $statusType) -eq 'closed') {
      continue
    }

    $fields = Build-FieldMap -Task $task
    $gate = Validate-Gates -Status $status -Fields $fields
    if ($null -eq $gate.gate) {
      continue
    }

    $evaluated += 1
    if ($gate.valid) {
      $results.Add([pscustomobject]@{
        action = 'no_change'
        list = $l.list
        task_id = $task.id
        task_name = $task.name
        status = $status
        gate = $gate.gate
        issues = @()
        url = "https://app.clickup.com/t/$($task.id)"
      }) | Out-Null
      continue
    }

    if ($Apply) {
      try {
        $body = @{ status = $gate.fallback } | ConvertTo-Json
        $update = Invoke-RestMethod -Method Put -Uri "https://api.clickup.com/api/v2/task/$($task.id)" -Headers $headers -Body $body

        $commentText = @(
          "[PDLC_GATE_ENFORCER] $($gate.gate) blocked.",
          "Invalid status transition detected for task in status '$status'.",
          "Task reverted to '$($gate.fallback)'.",
          'Missing criteria:',
          (@($gate.issues) | ForEach-Object { "- $_" })
        ) -join "`n"

        $commentBody = @{ comment_text = $commentText } | ConvertTo-Json
        Invoke-RestMethod -Method Post -Uri "https://api.clickup.com/api/v2/task/$($task.id)/comment" -Headers $headers -Body $commentBody | Out-Null

        $corrected += 1
        $results.Add([pscustomobject]@{
          action = 'reverted'
          list = $l.list
          task_id = $task.id
          task_name = $task.name
          from_status = $status
          to_status = $update.status.status
          gate = $gate.gate
          issues = @($gate.issues)
          url = "https://app.clickup.com/t/$($task.id)"
        }) | Out-Null
      }
      catch {
        $errorCount += 1
        $results.Add([pscustomobject]@{
          action = 'error'
          list = $l.list
          task_id = $task.id
          task_name = $task.name
          status = $status
          gate = $gate.gate
          issues = @($gate.issues)
          error = $_.Exception.Message
          url = "https://app.clickup.com/t/$($task.id)"
        }) | Out-Null
      }
    }
    else {
      $results.Add([pscustomobject]@{
        action = 'would_revert'
        list = $l.list
        task_id = $task.id
        task_name = $task.name
        status = $status
        target_status = $gate.fallback
        gate = $gate.gate
        issues = @($gate.issues)
        url = "https://app.clickup.com/t/$($task.id)"
      }) | Out-Null
    }
  }
}

$modeValue = 'dry-run'
if ($Apply) { $modeValue = 'apply' }

$summary = [ordered]@{
  mode = $modeValue
  scanned_tasks = [int]$scanned
  evaluated_tasks = [int]$evaluated
  corrected = [int]$corrected
  errors = [int]$errorCount
  results = $results
}

$summary | ConvertTo-Json -Depth 10
