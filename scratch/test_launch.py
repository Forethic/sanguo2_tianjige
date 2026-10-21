import subprocess, time, ctypes

p = subprocess.Popen(['D:/Software/Steam/steamapps/common/Sango2/LaunchSango.exe'], cwd='D:/Software/Steam/steamapps/common/Sango2')

EnumWindows = ctypes.windll.user32.EnumWindows
GetWindowThreadProcessId = ctypes.windll.user32.GetWindowThreadProcessId
GetWindowTextW = ctypes.windll.user32.GetWindowTextW
IsWindowVisible = ctypes.windll.user32.IsWindowVisible

for t in range(1, 15):
    time.sleep(1)
    hwnds = []
    def cb(hwnd, extra):
        buf = ctypes.create_unicode_buffer(256)
        GetWindowTextW(hwnd, buf, 256)
        title = buf.value
        if 'sango' in title.lower() or '群英' in title or 'odin' in title.lower():
            pid = ctypes.c_ulong()
            GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
            hwnds.append((hwnd, title, pid.value, bool(IsWindowVisible(hwnd))))
        return True

    EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_int, ctypes.c_int)(cb), 0)
    print(f"Sec {t}: poll={p.poll()}, hwnds={hwnds}")
    if any(h[3] for h in hwnds):
        break

if p.poll() is None:
    p.kill()
