import socket
import time
import os
import sys

# Dashboard Settings
PORT = 5555
DISPLAY_FREQ_HZ = 10
DRAW_THRESHOLD = 1.0 / DISPLAY_FREQ_HZ

# ANSI Color Codes
CYAN = "\033[36m"
YELLOW = "\033[33m"
GREEN = "\033[32m"
RED = "\033[31m"
GRAY = "\033[90m"
RESET = "\033[0m"
CLEAR = "\033[H\033[2J"

def format_f(val):
    try:
        return f"{float(val):.3f}"
    except:
        return "0.000"

def main():
    # Initialize UDP Socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("127.0.0.1", PORT))
    sock.setblocking(False)

    print(f"{CLEAR}{CYAN}==========================================")
    print("   NETH TELEMETRY DASHBOARD (PYTHON)")
    print(f"=========================================={RESET}")
    print(f"{GRAY} Listening on port {PORT}...{RESET}")

    last_draw = 0
    last_rate_update = time.time()
    packet_count = 0
    current_rate = 0
    last_packet_id = None
    dropped_packets = 0
    latest_content = None

    try:
        while True:
            # High-performance drain loop
            while True:
                try:
                    data, addr = sock.recvfrom(4096)
                    latest_content = data.decode('utf-8')
                    packet_count += 1
                    
                    # Track packet loss via PacketID
                    parts = latest_content.split('|', 1)
                    if len(parts) > 1:
                        try:
                            packet_id = int(parts[0])
                            if last_packet_id is not None and packet_id > (last_packet_id + 1):
                                dropped_packets += (packet_id - last_packet_id - 1)
                            last_packet_id = packet_id
                        except ValueError:
                            pass
                except BlockingIOError:
                    break

            now = time.time()
            
            # Update packet rate every second
            if now - last_rate_update >= 1.0:
                current_rate = packet_count
                packet_count = 0
                last_rate_update = now

            # Render dashboard at DISPLAY_FREQ_HZ
            if latest_content and (now - last_draw) >= DRAW_THRESHOLD:
                data = latest_content.split('|')
                
                # We expect 47 fields (indices 0 to 46)
                if len(data) >= 47:
                    # Extraction
                    p_id, name, t_stamp = data[0], data[1], data[2]
                    ias, tas, mach = data[3], data[39], data[40]
                    msl, agl, v_spd = data[4], data[41], data[42]
                    pitch, roll, hdg = data[5], data[6], data[7]
                    g_load, aoa, slip = data[8], data[9], data[10]
                    pos, vel, acc = data[11], data[12], data[13]
                    a_vel, a_acc, rates = data[14], data[15], data[16]
                    thr, i_pit, i_rol, i_yaw = data[17], data[18], data[19], data[20]
                    brk, flp, trm = data[21], data[22], data[23]
                    ags = data[24]
                    fuel, f_cap, f_flow = data[25], data[26], data[43]
                    mass, dmg, crit = data[27], data[28], data[44]
                    rpm1, rpm2, rpm3, rpm4 = data[30], data[31], data[32], data[33]
                    dens, temp, sos, q_press, drag = data[34], data[35], data[45], data[46], data[29]
                    gear, p_brk, vtol = data[36], data[37], data[38]

                    # Move cursor to top (line 5) to redraw without flickering
                    sys.stdout.write("\033[5;1H")
                    pad = " " * 20

                    print(f"{YELLOW} AIRCRAFT : {name} [{current_rate} Hz] | T: {t_stamp}s{pad}{RESET}")
                    print(f"{GRAY} COMMS    : PacketID: {p_id} | DROPPED: {dropped_packets}{pad}{RESET}")
                    print("-" * 42 + pad)
                    print(f"{GREEN} AIRSPEED : IAS:{format_f(ias)}  TAS:{format_f(tas)}  MACH:{format_f(mach)}{pad}{RESET}")
                    print(f"{GREEN} ALTIMET  : MSL:{format_f(msl)}m  AGL:{format_f(agl)}m  VSpd:{format_f(v_spd)}m/s{pad}{RESET}")
                    print(f"{CYAN} ORIENT   : P:{format_f(pitch)} R:{format_f(roll)} H:{format_f(hdg)}{pad}{RESET}")
                    print(f"{RED} AERO     : G:{format_f(g_load)} AOA:{format_f(aoa)} SLP:{format_f(slip)}{pad}{RESET}")
                    print("-" * 42 + pad)
                    print(f"{GRAY} LIN VEL  : {vel}{pad}{RESET}")
                    print(f"{GRAY} LIN ACC  : {acc}{pad}{RESET}")
                    print("-" * 42 + pad)
                    print(f"{YELLOW} THROTTLE : {format_f(thr)}  BRAKE: {format_f(brk)}  FLAPS: {format_f(flp)}{pad}{RESET}")
                    print(f"{YELLOW} EXTRA    : GEAR:{gear}  P.BRK:{p_brk}  VTOL:{format_f(vtol)}{pad}{RESET}")
                    print("-" * 42 + pad)
                    print(f"{GREEN} SYSTEMS  : FUEL:{format_f(fuel)}/{format_f(f_cap)}  FLOW:{format_f(f_flow)}  MASS:{format_f(mass)}kg{pad}{RESET}")
                    
                    dmg_color = RED if crit == "1" else GREEN
                    print(f"{dmg_color} DAMAGE   : DMG:{format_f(dmg)}  CRITICAL:{'YES' if crit == '1' else 'NO'}{pad}{RESET}")
                    
                    print(f"{CYAN} ENV/AERO : DRAG:{format_f(drag)}  Q:{format_f(q_press)}  DENS:{format_f(dens)}  SOS:{format_f(sos)}{pad}{RESET}")
                    print(f"{GRAY} ENGINES  : RPM1:{format_f(rpm1)}  RPM2:{format_f(rpm2)}  RPM3:{format_f(rpm3)}  RPM4:{format_f(rpm4)}{pad}{RESET}")
                    print("-" * 42 + pad)
                    
                    # AG State
                    ag_str = ""
                    for i in range(8):
                        state = ags[i] if i < len(ags) else "0"
                        color = GREEN if state == "1" else GRAY
                        ag_str += f"{color}AG{i+1} {RESET}"
                    print(f" AG STATE : {ag_str}{pad}")
                    print("-" * 42 + pad)
                    
                    last_draw = now

            time.sleep(0.001)

    except KeyboardInterrupt:
        print(f"\n{GRAY}Exiting...{RESET}")
    finally:
        sock.close()

if __name__ == "__main__":
    main()
