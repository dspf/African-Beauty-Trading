# Creates LocalDB database AfricanBeautyTradingCore if it does not exist
# Usage: powershell -ExecutionPolicy Bypass -File scripts/Create-LocalDbDatabase.ps1

$instance = "(localdb)\MSSQLLocalDB"
$database = "AfricanBeautyTradingCore"
$connectionString = "Server=$instance;Integrated Security=true;"

$query = "IF DB_ID(N'$database') IS NULL CREATE DATABASE [$database];"

try {
    Write-Output "Connecting to LocalDB instance: $instance"
    $conn = New-Object System.Data.SqlClient.SqlConnection $connectionString
    $cmd = $conn.CreateCommand()
    $cmd.CommandText = $query
    $conn.Open()
    $rows = $cmd.ExecuteNonQuery()
    $conn.Close()
    Write-Output "Database check/creation completed for '$database'."
} catch {
    Write-Error "Failed to create database '$database': $_"
    exit 1
} finally {
    if ($conn -ne $null -and $conn.State -eq 'Open') { $conn.Close() }
}
