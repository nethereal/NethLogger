$port = 5555
$udpClient = New-Object System.Net.Sockets.UdpClient($port)
$remoteEndPoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)

Clear-Host
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   NETH TELEMETRY DASHBOARD v9.0.0" -ForegroundColor Cyan
Write-Host "        (CLEAN OMNISCIENT) " -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host " Listening on port $port..." -ForegroundColor Gray

$lastDraw = [DateTime]::MinValue
$lastRateUpdate = Get-Date

# Display Settings
$displayFreqHz = 10
$drawThresholdMs = 1000 / $displayFreqHz

$packetCount = 0
$currentRate = 0
$lastPacketId = $null
$droppedPackets = 0

try {
    while($true) {
        $content = $null
        while ($udpClient.Available -gt 0) {
            $bytes = $udpClient.Receive([ref]$remoteEndPoint)
            $content = [System.Text.Encoding]::UTF8.GetString($bytes)
            $packetCount++
            
            $firstPipe = $content.IndexOf('|')
            if ($firstPipe -gt 0) {
                $packetIdStr = $content.Substring(0, $firstPipe)
                $packetId = 0
                if ([uint64]::TryParse($packetIdStr, [ref]$packetId)) {
                    if ($lastPacketId -ne $null -and $packetId -gt ($lastPacketId + 1)) {
                        $droppedPackets += ($packetId - $lastPacketId - 1)
                    }
                    $lastPacketId = $packetId
                }
            }
        }
        
        $now = Get-Date
        if ((New-TimeSpan -Start $lastRateUpdate -End $now).TotalSeconds -ge 1) {
            $currentRate = $packetCount
            $packetCount = 0
            $lastRateUpdate = $now
        }
        
        if ($content -and (New-TimeSpan -Start $lastDraw -End $now).TotalMilliseconds -gt $drawThresholdMs) {
            $data = $content.Split('|')
                    # Packet: PacketID(1) + Name(1) + Time(1) + Core(8) + LinVec(3) + RotVec(3) + Inputs(7) + AGs(1) + Systems(5) + Engine(4) + Env(2) + ExCtrl(3) + Adv(8) = 47
                    if ($data.Count -ge 47) {
                        $packetId = [uint64]$data[0]; $name = $data[1]; $time = [float]$data[2]
                        $ias = [float]$data[3]; $alt = [float]$data[4]; $pitch = [float]$data[5]; $roll = [float]$data[6]
                        $hdg = [float]$data[7]; $g = [float]$data[8]; $aoa = [float]$data[9]; $slip = [float]$data[10]
                        $pos = $data[11]; $vel = $data[12]; $acc = $data[13]
                        $aVel = $data[14]; $aAcc = $data[15]; $rates = $data[16]
                        $thr = [float]$data[17]; $iPit = [float]$data[18]; $iRol = [float]$data[19]; $iYaw = [float]$data[20]
                        $brk = [float]$data[21]; $flp = [float]$data[22]; $trm = [float]$data[23]
                        $ags = $data[24]
                        $fuel = [float]$data[25]; $fuelCap = [float]$data[26]; $mass = [float]$data[27]; $dmg = [float]$data[28]; $drag = [float]$data[29]
                        $rpm1 = [float]$data[30]; $rpm2 = [float]$data[31]; $rpm3 = [float]$data[32]; $rpm4 = [float]$data[33]
                        $airDen = [float]$data[34]; $temp = [float]$data[35]
                        $gear = $data[36]; $pBrake = $data[37]; $vtol = [float]$data[38]
                        $tas = [float]$data[39]; $mach = [float]$data[40]; $agl = [float]$data[41]; $vSpd = [float]$data[42]
                        $fuelFlow = [float]$data[43]; $critDmg = $data[44]; $sos = [float]$data[45]; $dynPress = [float]$data[46]

                        $curL = [Console]::CursorLeft
                        $curT = [Console]::CursorTop
                        [Console]::SetCursorPosition(0, 5)

                        $pad = "                                          "
                        
                        Write-Host (" AIRCRAFT : " + $name + " [" + $currentRate + " Hz] | T: " + $time.ToString("F3") + "s" + $pad) -ForegroundColor Yellow
                        Write-Host (" COMMS    : PacketID: " + $packetId + " | DROPPED: " + $droppedPackets + $pad) -ForegroundColor DarkGray
                        Write-Host "------------------------------------------"
                        Write-Host (" AIRSPEED : IAS:$($ias.ToString('F3'))  TAS:$($tas.ToString('F3'))  MACH:$($mach.ToString('F3'))" + $pad) -ForegroundColor Green
                        Write-Host (" ALTIMET  : MSL:$($alt.ToString('F3'))m  AGL:$($agl.ToString('F3'))m  VSpd:$($vSpd.ToString('F3'))m/s" + $pad) -ForegroundColor Green
                        Write-Host (" ORIENT   : P:$($pitch.ToString('F3')) R:$($roll.ToString('F3')) H:$($hdg.ToString('F3'))" + $pad) -ForegroundColor Cyan
                        Write-Host (" AERO     : G:$($g.ToString('F3')) AOA:$($aoa.ToString('F3')) SLP:$($slip.ToString('F3'))" + $pad) -ForegroundColor Red
                        Write-Host "------------------------------------------"
                        Write-Host (" LIN POS  : $pos" + $pad) -ForegroundColor Gray
                        Write-Host (" LIN VEL  : $vel" + $pad) -ForegroundColor Gray
                        Write-Host (" LIN ACC  : $acc" + $pad) -ForegroundColor Gray
                        Write-Host "------------------------------------------"
                        Write-Host (" ROT VEL  : $aVel" + $pad) -ForegroundColor Cyan
                        Write-Host (" ROT ACC  : $aAcc" + $pad) -ForegroundColor Cyan
                        Write-Host (" P/R/Y RT : $rates deg/s" + $pad) -ForegroundColor Cyan
                        Write-Host "------------------------------------------"
                        Write-Host (" THROTTLE : $($thr.ToString('F3'))  BRAKE: $($brk.ToString('F3'))  FLAPS: $($flp.ToString('F3'))" + $pad) -ForegroundColor Yellow
                        Write-Host (" STICK    : P:$($iPit.ToString('F3'))  R:$($iRol.ToString('F3'))  Y:$($iYaw.ToString('F3'))  T:$($trm.ToString('F3'))" + $pad) -ForegroundColor Yellow
                        Write-Host (" EXTRA    : GEAR:$gear  P.BRK:$pBrake  VTOL:$($vtol.ToString('F3'))" + $pad) -ForegroundColor Yellow
                        Write-Host "------------------------------------------"
                        Write-Host (" SYSTEMS  : FUEL:$($fuel.ToString('F3'))/$($fuelCap.ToString('F3'))  FLOW:$($fuelFlow.ToString('F3'))  MASS:$($mass.ToString('F3'))kg" + $pad) -ForegroundColor Green
                        Write-Host (" DAMAGE   : DMG:$($dmg.ToString('F3'))  CRITICAL:$critDmg" + $pad) -ForegroundColor Red
                        Write-Host (" ENV/AERO : DRAG:$($drag.ToString('F3'))  Q:$($dynPress.ToString('F3'))  DENS:$($airDen.ToString('F3'))  TEMP:$($temp.ToString('F3'))  SOS:$($sos.ToString('F3'))" + $pad) -ForegroundColor Cyan
                        Write-Host (" ENGINES  : RPM1:$($rpm1.ToString('F3'))  RPM2:$($rpm2.ToString('F3'))  RPM3:$($rpm3.ToString('F3'))  RPM4:$($rpm4.ToString('F3'))" + $pad) -ForegroundColor Gray
                        Write-Host "------------------------------------------"
                        Write-Host -NoNewline " AG STATE : "
                        for($i=0; $i -lt 8; $i++) {
                            $c = "0"
                            if ($i -lt $ags.Length) { $c = $ags[$i].ToString() }
                            if ($c -eq "1") {
                                Write-Host -NoNewline " AG$($i+1)" -ForegroundColor Green
                            } else {
                                Write-Host -NoNewline " AG$($i+1)" -ForegroundColor DarkGray
                            }
                        }
                        Write-Host ($pad)
                        Write-Host "------------------------------------------"
                        Write-Host (" RAW      : $content" + $pad) -ForegroundColor DarkGray
                        Write-Host ("------------------------------------------" + $pad)

                        [Console]::SetCursorPosition($curL, $curT)
                        $lastDraw = $now
                    }
        }
        Start-Sleep -Milliseconds 1
    }
} finally {
    $udpClient.Close()
}
