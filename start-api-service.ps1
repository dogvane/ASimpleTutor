# 启动ASimpleTutor API服务的脚本

# 设置工作目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

# 输出当前目录
Write-Host "当前工作目录: $pwd"

# 启动API服务
Write-Host "正在启动ASimpleTutor API服务..."

# 运行API服务
try {
    # 检查是否已存在运行中的服务进程
    $runningProcesses = Get-Process | Where-Object {$_.ProcessName -eq "ASimpleTutor.Api"}
    if ($runningProcesses.Count -gt 0) {
        Write-Host "发现已存在的ASimpleTutor.Api进程，正在停止..."
        $runningProcesses | ForEach-Object { $_.Kill() }
        Start-Sleep -Seconds 2
    }

    # 启动新的服务进程
    Write-Host "启动新的ASimpleTutor API服务进程..."
    $apiProjectPath = Join-Path $scriptDir "src\ASimpleTutor.Api"
    Set-Location $apiProjectPath
    
    # 运行API服务，使用后台进程运行
    Start-Process "dotnet" -ArgumentList "run" -WindowStyle Minimized
    
    Write-Host "ASimpleTutor API服务已启动"
    Write-Host "服务地址: http://localhost:5000"
    Write-Host "Swagger文档: http://localhost:5000/swagger"
    
    # 等待服务启动
    Write-Host "正在等待服务启动..."
    Start-Sleep -Seconds 5
    
    # 检查服务是否启动成功
    try {
        $response = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get
        Write-Host "服务启动成功，健康检查状态: $($response.status)"
    } catch {
        Write-Host "服务启动可能失败，请检查终端输出"
    }
    
} catch {
    Write-Host "启动服务时出错: $($_.Exception.Message)"
}

# 返回脚本目录
Set-Location $scriptDir
Write-Host "脚本执行完成"
