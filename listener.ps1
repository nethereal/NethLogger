# Neth Telemetry UDP Listener (High Precision v2.7.1)
# Listens on 127.0.0.1:13434 at 25Hz

$Port = 13434
$UdpClient = New-Object System.Net.Sockets.UdpClient($Port)
$RemoteEndPoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)

# Set low priority for this process
(Get-Process -Id $PID).PriorityClass = 'BelowNormal'

$LastUIUpdate = [DateTime]::MinValue
$UpdateIntervalMs = 40 
$PacketCount = 0
$CurrentPPS = 0
$LastPPSUpdate = Get-Date

# Initial Screen Setup
Clear-Host
Write-Host "========================================" -ForegroundColor Green
Write-Host "       NETH HIGH-PRECISION v2.7.1       " -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host " Listening on port $Port..."
Write-Host ""

$StartRow = [Console]::CursorTop

try {
    while ($true) {
        $Bytes = $UdpClient.Receive([ref]$RemoteEndPoint)
        $Data = [System.Text.Encoding]::UTF8.GetString($Bytes)
        $PacketCount++
        
        $Now = Get-Date
        if (($Now - $LastPPSUpdate).TotalSeconds -ge 1.0) {
            $CurrentPPS = $PacketCount
            $PacketCount = 0
            $LastPPSUpdate = $Now
        }

        if (($Now - $LastUIUpdate).TotalMilliseconds -gt $UpdateIntervalMs) {
            if ($Data.StartsWith("NAV|")) {
                $Sections = $Data.Split('|')
                $Pos = $Sections[1].Split(',')
                $Att = $Sections[2].Split(',')
                $Nav = $Sections[3].Split(',')
                $Vecs = $Sections[4].Split(';')
                $Mass = $Sections[5]
                $GForce = $Sections[6]
                
                $TAS = "{0:N2}" -f [double]$Nav[0]
                $AGL = "{0:N2}" -f [double]$Nav[1]
                $HDG = "{0:N2}" -f [double]$Att[1]
                
                $Fwd = $Vecs[0]
                $Up = $Vecs[1]
                $Right = $Vecs[2]

                [Console]::SetCursorPosition(0, $StartRow)
                Write-Host "  NET PPS  : $CurrentPPS".PadRight(30)
                Write-Host "----------------------------------------"
                Write-Host "  TAS      : $TAS MPH".PadRight(30) -ForegroundColor Cyan
                Write-Host "  AGL      : $AGL FT".PadRight(30) -ForegroundColor Green
                Write-Host "  HEADING  : $HDG°".PadRight(30) -ForegroundColor Yellow
                Write-Host "----------------------------------------"
                Write-Host "  G-LOAD   : $GForce G  |  MASS: $Mass KG".PadRight(60)
                Write-Host "----------------------------------------"
                Write-Host "  FORWARD  : ($Fwd)".PadRight(60)
                Write-Host "  UP       : ($Up)".PadRight(60)
                Write-Host "  RIGHT    : ($Right)".PadRight(60)
                Write-Host "----------------------------------------"
                Write-Host "  POSITION : ($($Pos[0]), $($Pos[1]), $($Pos[2]))".PadRight(60)
                Write-Host "  ATTITUDE : P:$($Att[0]) R:$($Att[1])".PadRight(60)
                Write-Host "========================================" -ForegroundColor Green
                
                $LastUIUpdate = $Now
            }
        }
    }
}
catch {
    Write-Host "`nListener stopped."
}
finally {
    $UdpClient.Close()
}
