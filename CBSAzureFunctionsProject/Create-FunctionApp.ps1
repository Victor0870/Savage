# —————— Create-FunctionApp.ps1 ——————

# 1) Дозволити виконання скриптів у цьому процесі
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

# 2) Зупиняти скрипт при будь-якій помилці
$ErrorActionPreference = 'Stop'

# 3) Вивести всі доступні підписки
Write-Host "`nAvailable Azure Subscriptions:`n" -ForegroundColor Cyan
az account list --query '[].{Name:name,Id:id}' --output table

# 4) Запитати у користувача, яку підписку використовувати
$subscription = Read-Host "`nEnter Subscription Name or ID to use"
az account set --subscription $subscription
Write-Host "Using subscription:`t$subscription`n" -ForegroundColor Green

# 5) Запитати ім’я Function App, якщо не передано аргументом
if (-not $FunctionAppName) {
    $FunctionAppName = Read-Host "Enter Function App name (letters/numbers only, no spaces)"
}

# 6) Підготовка імен
$location = 'westus2'
$rg       = "$FunctionAppName-rg"

# Згенерувати валідне имя storage (тільки a–z, 0–9, до 24 символів)
$clean = ($FunctionAppName.ToLower() -replace '[^a-z0-9]','')
if ($clean.Length -gt 18) { $clean = $clean.Substring(0,18) }
$storage = "${clean}storage"

# 7) Створення ресурсів
Write-Host "➜ Creating resource group $rg..."
az group create --name $rg --location $location

Write-Host "➜ Creating storage account $storage..."
az storage account create `
  --name $storage `
  --resource-group $rg `
  --location $location `
  --sku Standard_LRS

Write-Host "➜ Creating Function App $FunctionAppName in Flex Consumption plan..."
az functionapp create `
  --name $FunctionAppName `
  --resource-group $rg `
  --storage-account $storage `
  --flexconsumption-location $location `
  --runtime dotnet-isolated `
  --runtime-version 8.0 `
  --functions-version 4 `
  --instance-memory 2048

Write-Host "`n✅ Function App created: $FunctionAppName"
# ——————————————————————————————
