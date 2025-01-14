using module @{ ModuleName = "Microsoft.Graph.Applications"; ModuleVersion = "1.6.0"; MaximumVersion = "1.99.99" }
using module @{ ModuleName = "Microsoft.Graph.Authentication"; ModuleVersion = "1.6.0"; MaximumVersion = "1.99.99" }
using module @{ ModuleName = "Az.Accounts"; ModuleVersion = "3.0.4"; MaximumVersion = "3.99.99" }
using module @{ ModuleName = "Az.Resources"; ModuleVersion = "7.4.0"; MaximumVersion = "7.99.99" }

param(
    [Parameter(Mandatory)]
    [Guid]
    $TenantId,
    [Parameter(Mandatory, HelpMessage="The Kerbee application id.")]
    [Guid]
    $AppId
)

Connect-AzAccount -Tenant $TenantId

$kerbeePrincipal = Get-AzADServicePrincipal -Filter "appId eq '$AppId'"
$msGraphServicePrincipal = Get-AzADServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'"

$appRole = $msGraphServicePrincipal.AppRole |
    Where-Object {($_.Value -eq "Application.ReadWrite.OwnedBy") -and ($_.AllowedMemberType -contains "Application")}

Connect-MgGraph -TenantId $TenantId

$appRoleAssignment = @{
    PrincipalId = $kerbeePrincipal.Id
    ResourceId = $msGraphServicePrincipal.Id
    AppRoleId = $appRole.Id
}

New-MgServicePrincipalAppRoleAssignment `
     -ServicePrincipalId $appRoleAssignment.PrincipalId `
     -BodyParameter $appRoleAssignment `
     -Verbose