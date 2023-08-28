# Getting Started

## Deploy Kerbee

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FWeAreInSpark%2Fkey-rotation-bot%2Fadd-arm-file%2Fazuredeploy.json)

## Add application settings

Update the following configuration settings of the Function App:

- `Kerbee:ValidityInMonths` - The validity of the certificate in months. The default is 3 months.
- `Kerbee:RenewBeforeExpiryInDays` - The number of days before expiry to renew the certificate. The default is 30 days.
- `Kerbee:DefaultKeyType` - The default key type. The options are `Certificate` or `Secret`. The default is `Certificate`.

## Enable App Service Authentication

You must enable Authentication on the Function App that is deployed as part of this application.

In the Azure Portal, open the Function blade then select the `Authentication`` menu and enable App Service authentication. Click on the `Add identity provider`` button to display the screen for adding a new identity provider. If you select `Microsoft`` as your Identity provider, the required settings will be automatically filled in for you. The default settings are fine.

![Add an Identity provider](images/add-identity-provider.png)

Make sure that the App Service Authentication setting is set to `Require authentication``. The permissions can basically be left at the default settings.

![App Service Authentication settings](images/app-service-authentication-settings.png)

If you are using Sovereign Cloud, you may not be able to select Express. Enable authentication from the advanced settings with reference to the following document.

https://docs.microsoft.com/en-us/azure/app-service/configure-authentication-provider-aad#-configure-with-advanced-settings

Finally, you can save your previous settings to enable App Service authentication.