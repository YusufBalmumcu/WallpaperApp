import ctypes
import time
from ctypes import windll, byref, c_int, c_uint, c_void_p, Structure, sizeof, c_wchar_p, c_longlong, c_ulonglong, POINTER
from ctypes import wintypes

# --- TYPES ---
LRESULT = c_longlong
WPARAM = c_ulonglong
LPARAM = c_longlong
HANDLE = c_void_p

# --- CONSTANTS ---
WS_POPUP = 0x80000000
WS_VISIBLE = 0x10000000
WM_DESTROY = 0x0002
WM_PAINT = 0x000F
WM_ERASEBKGND = 0x0014
SWP_NOACTIVATE = 0x0010
SWP_SHOWWINDOW = 0x0040
SM_XVIRTUALSCREEN = 76
SM_YVIRTUALSCREEN = 77
SM_CXVIRTUALSCREEN = 78
SM_CYVIRTUALSCREEN = 79
IDC_ARROW = 32512
HWND_TOPMOST = -1

# --- LOAD DLLs ---
user32 = windll.user32
kernel32 = windll.kernel32
gdi32 = windll.gdi32

# Define Argtypes
user32.DefWindowProcW.argtypes = [HANDLE, c_uint, WPARAM, LPARAM]
user32.DefWindowProcW.restype = LRESULT
user32.BeginPaint.argtypes = [HANDLE, c_void_p]
user32.BeginPaint.restype = HANDLE
user32.EndPaint.argtypes = [HANDLE, c_void_p]
user32.FillRect.argtypes = [HANDLE, c_void_p, HANDLE]

# --- STRUCTS ---
class PAINTSTRUCT(Structure):
    _fields_ = [("hdc", HANDLE), ("fErase", c_int), ("rcPaint", wintypes.RECT),
                ("fRestore", c_int), ("fIncUpdate", c_int), ("rgbReserved", c_int * 32)]

class WNDCLASSEX(Structure):
    _fields_ = [("cbSize", c_int), ("style", c_int),
                ("lpfnWndProc", ctypes.WINFUNCTYPE(LRESULT, HANDLE, c_uint, WPARAM, LPARAM)),
                ("cbClsExtra", c_int), ("cbWndExtra", c_int), ("hInstance", HANDLE), ("hIcon", HANDLE),
                ("hCursor", HANDLE), ("hbrBackground", HANDLE), ("lpszMenuName", c_wchar_p),
                ("lpszClassName", c_wchar_p), ("hIconSm", HANDLE)]

# Keep ref
WNDPROC_REF = None

def main():
    global WNDPROC_REF
    
    # 1. Coordinates
    x = user32.GetSystemMetrics(SM_XVIRTUALSCREEN)
    y = user32.GetSystemMetrics(SM_YVIRTUALSCREEN)
    w = user32.GetSystemMetrics(SM_CXVIRTUALSCREEN)
    h = user32.GetSystemMetrics(SM_CYVIRTUALSCREEN)
    print(f"Goal: {w}x{h} at {x},{y}")

    # 2. BRUSH (Red)
    h_brush = gdi32.CreateSolidBrush(0x0000FF)

    # 3. Window Procedure (THE FIX IS HERE)
    def wnd_proc(hwnd, msg, wParam, lParam):
        if msg == WM_DESTROY:
            user32.PostQuitMessage(0)
            return 0
        
        # MANUALLY PAINT THE WINDOW (Prevents Transparency)
        elif msg == WM_PAINT:
            ps = PAINTSTRUCT()
            hdc = user32.BeginPaint(hwnd, byref(ps))
            rect = wintypes.RECT(0, 0, w, h)
            user32.FillRect(hdc, byref(rect), h_brush)
            user32.EndPaint(hwnd, byref(ps))
            return 0
            
        # IGNORE ERASE BACKGROUND (Prevents flickering/transparency)
        elif msg == WM_ERASEBKGND:
            return 1 

        return user32.DefWindowProcW(hwnd, msg, wParam, lParam)

    WNDPROC_REF = ctypes.WINFUNCTYPE(LRESULT, HANDLE, c_uint, WPARAM, LPARAM)(wnd_proc)
    
    h_inst = kernel32.GetModuleHandleW(None)
    wnd_class = WNDCLASSEX()
    wnd_class.cbSize = sizeof(WNDCLASSEX)
    wnd_class.style = 3 # CS_HREDRAW | CS_VREDRAW
    wnd_class.lpfnWndProc = WNDPROC_REF
    wnd_class.hInstance = h_inst
    wnd_class.hCursor = user32.LoadCursorW(0, IDC_ARROW)
    wnd_class.hbrBackground = h_brush
    wnd_class.lpszClassName = "ForcePaintClass"
    
    user32.RegisterClassExW(byref(wnd_class))

    # 4. Create Window
    print("PHASE 1: Creating Window...")
    hwnd = user32.CreateWindowExW(
        0, "ForcePaintClass", "Test",
        WS_POPUP | WS_VISIBLE,
        x, y, w, h,
        None, None, h_inst, None
    )
    
    user32.SetWindowPos(hwnd, HWND_TOPMOST, x, y, w, h, SWP_SHOWWINDOW)
    
    # 5. Attach Logic
    start_time = time.time()
    attached = False
    msg = wintypes.MSG()

    while True:
        while user32.PeekMessageW(byref(msg), None, 0, 0, 1):
            user32.TranslateMessage(byref(msg))
            user32.DispatchMessageW(byref(msg))
        
        if not attached and (time.time() - start_time > 3):
            print("PHASE 2: Attaching...")
            progman = user32.FindWindowW("Progman", None)
            icon_view = user32.FindWindowExW(progman, 0, "SHELLDLL_DefView", None)
            
            if progman and icon_view:
                user32.SetParent(hwnd, progman)
                
                # INSERT BEHIND ICONS
                user32.SetWindowPos(hwnd, icon_view, 0, 0, w, h, SWP_SHOWWINDOW)
                
                # FORCE REPAINT IMMEDIATELY
                user32.RedrawWindow(hwnd, None, None, 5) # RDW_INVALIDATE | RDW_UPDATENOW
                print("Attached. Should be RED behind icons.")
            attached = True
            
        time.sleep(0.01)

if __name__ == "__main__":
    main()