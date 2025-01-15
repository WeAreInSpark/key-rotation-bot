#!/bin/bash

# Ensure the script exits if any command fails
set -e

# Parameters
TENANT_ID=$1
APP_ID=$2

# Login to Azure
az login --tenant $TENANT_ID

# Get the service principal for the application
KERBEE_PRINCIPAL_ID=$(az ad sp list --filter "appId eq '$APP_ID'" --query "[0].objectId" -o tsv)

# Get the Microsoft Graph service principal
MS_GRAPH_SP_ID=$(az ad sp list --filter "appId eq '00000003-0000-0000-c000-000000000000'" --query "[0].objectId" -o tsv)

# Get the app role ID for "Application.ReadWrite.OwnedBy"
APP_ROLE_ID=$(az ad sp show --id $MS_GRAPH_SP_ID --query "appRoles[?value=='Application.ReadWrite.OwnedBy' && contains(allowedMemberTypes, 'Application')].id" -o tsv)

# Assign the app role to the service principal
az ad sp create --id $KERBEE_PRINCIPAL_ID --role $APP_ROLE_ID --scope $MS_GRAPH_SP_ID

echo "App role assigned successfully."