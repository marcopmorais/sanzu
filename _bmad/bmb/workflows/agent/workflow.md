# BMAD Agent Builder Workflow

**Modes:** Create (CA) | Edit (EA) | Validate (VA)

Determine which mode was invoked from the menu command that loaded this file, then follow the corresponding section. If unclear, ask the user.

---

## Mode Detection

| Command | Mode |
|---------|------|
| CA / create-agent | [A] CREATE — scaffold a new BMAD agent |
| EA / edit-agent   | [B] EDIT — modify an existing agent |
| VA / validate-agent | [C] VALIDATE — compliance check an agent |

---

## [A] CREATE — New BMAD Agent

### Step 1: Discover requirements

Ask the user:
1. What is this agent's name (display name) and persona name?
2. What module does it belong to? (`core`, `bmm`, `bmb`, `cis`, or other)
3. Which config.yaml should it load? (`_bmad/bmm/config.yaml` for BMM agents, `_bmad/bmb/config.yaml` for BMB agents)
4. What is the agent's primary role in one sentence?
5. What workflows will appear in the menu? (list them with workflow paths or exec paths)
6. Does this agent own any output artifacts? If so, list them with paths and descriptions.
7. Which other agents does this agent interact with?

### Step 2: Design agent structure

Apply all BMAD agent compliance rules:

**Frontmatter (YAML):**
- `name:` — kebab-case identifier matching filename
- `description:` — short human-readable description

**Preamble:** Standard preamble instructing the agent to embody the persona and never break character.

**XML block structure:**
```
<agent id="{name}.agent.yaml" name="{persona_name}" title="{title}" icon="{icon}">
<activation critical="MANDATORY">
  <step n="1">Load persona from this current agent file (already in context)</step>
  <step n="2">🚨 IMMEDIATE ACTION REQUIRED ... load config.yaml ... store session variables</step>
  <step n="3">Remember: user's name is {user_name}</step>
  <step n="4">Find if this exists, treat as bible: `**/project-context.md`</step>
  <step n="5">Show greeting, communicate in {communication_language}, display menu</step>
  <step n="6">STOP and WAIT for user input</step>
  <step n="7">On user input: Number → execute | Text → fuzzy match | Multiple → clarify | No match → show "Not recognized"</step>
  <step n="8">When executing: check menu-handlers, extract attributes, follow handler instructions</step>

  <menu-handlers>
    <handlers>
      <handler type="workflow">...</handler>  <!-- include if any menu item uses workflow= -->
      <handler type="exec">...</handler>      <!-- include if any menu item uses exec= -->
    </handlers>
  </menu-handlers>

  <rules>
    <r>Follow {project-root}/_bmad/policies/clickup-os-policy.md ...</r>  <!-- FIRST rule always -->
    <r>Follow docs/tooling_contract.md. ClickUp and Figma integrations are removed...</r>
    <r>ALWAYS communicate in {communication_language} UNLESS contradicted by communication_style.</r>
    <!-- TTS rule as markdown bullet (not <r> tag) -->
    - When responding to user messages, speak your responses using TTS: ...
    <r>Stay in character until exit selected</r>
    <r>Display Menu items as the item dictates and in the order given.</r>
    <r>Load files ONLY when executing a user chosen workflow or a command requires it, EXCEPTION: agent activation step 2 config.yaml</r>
    <r>Artifact ownership: ...</r>          <!-- if agent owns artifacts -->
    <r>Cross-agent interactions: ...</r>    <!-- if agent interacts with others -->
  </rules>
</activation>

<persona>
  <role>...</role>
  <identity>...</identity>
  <communication_style>...</communication_style>
  <principles>...</principles>
  <artifacts>
    <owns>...</owns>
    <reads>...</reads>
  </artifacts>
  <interactions>...</interactions>
</persona>

<menu>
  <item cmd="MH or fuzzy match on menu or help">[MH] Redisplay Menu Help</item>
  <item cmd="CH or fuzzy match on chat">[CH] Chat with the Agent about anything</item>
  <!-- workflow and exec items -->
  <item cmd="PM or fuzzy match on party-mode" exec="{project-root}/_bmad/core/workflows/party-mode/workflow.md">[PM] Start Party Mode</item>
  <item cmd="DA or fuzzy match on exit, leave, goodbye or dismiss agent">[DA] Dismiss Agent</item>
</menu>
</agent>
```

**Standard menu handler text (workflow):**
```xml
<handler type="workflow">
  When menu item has: workflow="path/to/workflow.yaml":
  1. CRITICAL: Always LOAD {project-root}/_bmad/core/tasks/workflow.xml
  2. Read the complete file - this is the CORE OS for executing BMAD workflows
  3. Pass the yaml path as 'workflow-config' parameter to those instructions
  4. Execute workflow.xml instructions precisely following all steps
  5. Save outputs after completing EACH workflow step (never batch multiple steps together)
  6. If workflow.yaml path is "todo", inform user the workflow hasn't been implemented yet
</handler>
```

**Standard menu handler text (exec):**
```xml
<handler type="exec">
  When menu item or handler has: exec="path/to/file.md":
  1. Actually LOAD and read the entire file and EXECUTE the file at that path - do not improvise
  2. Read the complete file and follow all instructions within it
  3. If there is data="some/path/data-foo.md" with the same item, pass that data path to the executed file as context.
</handler>
```

### Step 3: Draft and confirm

Write the complete agent file content and show it to the user before saving.

Ask: "Does this agent definition look correct? I'll save it once confirmed."

### Step 4: Save and register

1. Determine the path: `{project-root}/_bmad/{module}/agents/{name}.md`
2. Write the file.
3. Add the agent to `{project-root}/_bmad/_config/agent-manifest.csv` with all fields.
4. Create a slash command at `.claude/commands/bmad-agent-{name}.md` with the standard bootstrap pattern:
   ```
   Load and activate the {title} agent from `_bmad/{module}/agents/{name}.md`. Read the entire file and follow all activation instructions exactly, including loading config.yaml and displaying the menu.
   ```

---

## [B] EDIT — Modify an Existing Agent

### Step 1: Load target agent

Ask: "Which agent file should I edit? (provide path or agent name)"

Load the complete agent file.

### Step 2: Understand change intent

Ask: "What should I change? Options:
- (A) Add or modify a menu item
- (B) Update persona (role, identity, communication style, principles)
- (C) Add or update artifact ownership declarations
- (D) Update cross-agent interactions
- (E) Fix a compliance issue
- (F) Other — describe"

### Step 3: Apply changes with validation

Before editing, validate that:
- ClickUp OS policy remains the first `<r>` in rules (if it was present)
- Activation steps 1–8 remain in sequence
- All menu handlers present match the handler types used in the menu
- `project-context.md` lookup step is present (step 4)

Show the changed sections and confirm before saving.

### Step 4: Save and report

Write the updated file. Check if agent-manifest.csv needs updating for any field changes.

---

## [C] VALIDATE — Compliance Check

### Step 1: Identify target

Ask: "Which agent(s) should I validate? Options:
- (A) A specific agent — provide path
- (B) All BMM agents
- (C) All registered agents (from agent-manifest.csv)"

Load each agent file for validation.

### Step 2: Run compliance checklist

For each agent, check — mark ✅ PASS / ❌ FAIL / ⚠️ WARN:

**Frontmatter:**
- [ ] YAML frontmatter present with `name:` and `description:` fields
- [ ] `name` matches the filename (without `.md`)

**Preamble:**
- [ ] Preamble text present instructing agent to embody persona

**XML block — `<agent>` tag:**
- [ ] Has `id`, `name`, `title`, `icon` attributes

**Activation steps:**
- [ ] Step 1: Load persona (already in context)
- [ ] Step 2: Load config.yaml immediately, store `{user_name}`, `{communication_language}`, `{output_folder}`, VERIFY and STOP on fail
- [ ] Step 3: Remember user's name
- [ ] Step 4: Find `**/project-context.md` and treat as bible
- [ ] Step 5: Show greeting with `{user_name}`, display menu in `{communication_language}`
- [ ] Step 6: STOP and WAIT for user input
- [ ] Step 7: Input routing (number, text, multiple, no match)
- [ ] Step 8: Menu-handler dispatch

**Menu handlers:**
- [ ] `<menu-handlers>` block present inside `<activation>`
- [ ] `<handler type="workflow">` present if any menu item uses `workflow=` attribute
- [ ] `<handler type="exec">` present if any menu item uses `exec=` attribute
- [ ] Handler text matches the standard canonical text

**Rules:**
- [ ] ClickUp OS policy rule is the FIRST `<r>` (except for BMB agents if not applicable)
- [ ] tooling_contract.md rule present
- [ ] Communication language rule present
- [ ] TTS rule present (as markdown bullet, NOT `<r>` tag)
- [ ] Stay in character rule present
- [ ] Display menu order rule present
- [ ] Load files only on demand rule present
- [ ] Artifact ownership rule present (if agent owns artifacts)
- [ ] Cross-agent interactions rule present (if agent has interactions)

**Persona:**
- [ ] `<role>` present and specific
- [ ] `<identity>` present with authentic backstory
- [ ] `<communication_style>` present with concrete behavioral description
- [ ] `<principles>` present with actionable principles
- [ ] `<artifacts>` block present if agent owns or reads output files
  - [ ] Each `<artifact>` has `path` and `description` attributes
  - [ ] Owned paths use `{output_folder}` variable (not hardcoded)
- [ ] `<interactions>` block present if agent coordinates with others
  - [ ] Each `<with>` has `agent=` attribute pointing to real agent file

**Menu:**
- [ ] MH (menu help) item first
- [ ] CH (chat) item second
- [ ] PM (party mode) item second-to-last
- [ ] DA (dismiss agent) item last
- [ ] All workflow paths reference real files that exist
- [ ] All exec paths reference real files that exist

**Registration:**
- [ ] Agent present in `_bmad/_config/agent-manifest.csv`
- [ ] Slash command exists at `.claude/commands/bmad-agent-{name}.md`

### Step 3: Report findings per agent

```
VALIDATION REPORT: {agent_file}
================================
PASS: N items
WARN: N items
FAIL: N items

Issues:
[list each FAIL/WARN with description and recommended fix]

Verdict: COMPLIANT | NEEDS FIXES
```

After all agents, produce a summary table:

```
AGENT COMPLIANCE SUMMARY
========================
Agent       | Pass | Warn | Fail | Verdict
-----------|------|------|------|--------
{name}     | N    | N    | N    | ✅/❌
```

### Step 4: Offer fixes

For each non-compliant agent, ask:
"Should I fix {agent_name} now? I can apply all FAIL items automatically."

If yes, apply all fixes, save, and re-run the checklist to confirm all items pass.
