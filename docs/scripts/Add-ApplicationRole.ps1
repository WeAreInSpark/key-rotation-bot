param(
    [Parameter(Mandatory)]
    [Guid]
    $TenantId,
    [Parameter(Mandatory, HelpMessage = "The Kerbee application id.")]
    [Guid]
    $AppId
)

# Login to Azure
az login --tenant $TenantId

# Get the service principal for the application
$kerbeePrincipalId = az ad sp show --id $AppId --query "id" --output tsv

# Get the Microsoft Graph service principal
$msGraphServicePrincipalId = az ad sp show --id '00000003-0000-0000-c000-000000000000' --query "id" --output tsv

# Get the app role ID for "Application.ReadWrite.OwnedBy"
$appRoleId = az ad sp show --id $msGraphServicePrincipalId --query "appRoles[?value=='Application.ReadWrite.OwnedBy' && contains(allowedMemberTypes, 'Application')].id" -o tsv

# Assign the app role to the service principal
az ad sp create --id $kerbeePrincipalId --role $appRoleId --scope $msGraphServicePrincipalId