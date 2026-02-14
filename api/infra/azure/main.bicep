@description('Azure region for all resources')
param location string

@description('App Service plan name')
param appServicePlanName string

@description('API web app name')
param apiWebAppName string

@description('Frontend web app name')
param frontendWebAppName string

@description('Azure SQL logical server name')
param sqlServerName string

@description('Azure SQL database name')
param sqlDatabaseName string

@description('Azure SQL admin login username')
param sqlAdminLogin string

@secure()
@description('Azure SQL admin login password')
param sqlAdminPassword string

var sqlConnectionString = 'Server=tcp:${sqlServerName}.database.windows.net,1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

resource apiWebApp 'Microsoft.Web/sites@2023-12-01' = {
  name: apiWebAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|9.0'
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'ConnectionStrings__DefaultConnection'
          value: sqlConnectionString
        }
      ]
    }
    httpsOnly: true
  }
}

resource frontendWebApp 'Microsoft.Web/sites@2023-12-01' = {
  name: frontendWebAppName
  location: location
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: 'NODE|20-lts'
      alwaysOn: true
      appSettings: [
        {
          name: 'SCM_DO_BUILD_DURING_DEPLOYMENT'
          value: 'true'
        }
        {
          name: 'ENABLE_ORYX_BUILD'
          value: 'true'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~20'
        }
        {
          name: 'API_BASE_URL'
          value: 'https://${apiWebApp.properties.defaultHostName}'
        }
      ]
    }
    httpsOnly: true
  }
}

resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  name: '${sqlServer.name}/${sqlDatabaseName}'
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}

resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  name: '${sqlServer.name}/AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

output apiWebAppName string = apiWebApp.name
output frontendWebAppName string = frontendWebApp.name
output apiUrl string = 'https://${apiWebApp.properties.defaultHostName}'
output frontendUrl string = 'https://${frontendWebApp.properties.defaultHostName}'
output sqlServerFqdn string = '${sqlServer.name}.database.windows.net'
