import ctypes
from ctypes import windll

# --- Constants ---
WM_SPAWN_WORKER = 0x052C

def get_class_name(hwnd):
    buff = ctypes.create_unicode_buffer(256)
    windll.user32.GetClassNameW(hwnd, buff, 256)
    return buff.value

def print_hierarchy():
    user32 = windll.user32
    
    print("\n--- DESKTOP WINDOW HIERARCHY ---")
    
    # 1. Send the magic message to ensure the structure exists
    print("[*] Sending magic message 0x052C to Progman...")
    progman = user32.FindWindowW("Progman", None)
    user32.SendMessageTimeoutW(progman, WM_SPAWN_WORKER, 0, 0, 0, 1000, ctypes.byref(ctypes.c_void_p()))
    
    # 2. Find all Top-Level Windows
    top_windows = []
    def enum_cb(hwnd, lParam):
        top_windows.append(hwnd)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, ctypes.c_void_p, ctypes.c_void_p)(enum_cb), 0)

    # 3. Filter for Progman and WorkerW
    relevant_windows = []
    for hwnd in top_windows:
        cls = get_class_name(hwnd)
        if cls in ["Progman", "WorkerW"]:
            relevant_windows.append(hwnd)

    # 4. Print the Tree
    for hwnd in relevant_windows:
        cls = get_class_name(hwnd)
        print(f"Window Handle: {hwnd} | Class: {cls}")
        
        # Check for Children (Look for the Icons: SHELLDLL_DefView)
        child = user32.FindWindowExW(hwnd, 0, None, None)
        while child:
            child_cls = get_class_name(child)
            print(f"    └── Child Handle: {child} | Class: {child_cls}")
            
            if child_cls == "SHELLDLL_DefView":
                print("        [!!!] FOUND ICONS HERE [!!!]")
            
            child = user32.FindWindowExW(hwnd, child, None, None)
            
    print("--------------------------------\n")

if __name__ == "__main__":
    print_hierarchy()