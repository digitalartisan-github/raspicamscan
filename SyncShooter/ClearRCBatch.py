import tempfile
import os


dir = tempfile.gettempdir()
lock_path =  os.path.join(dir,'_rc_bacth_lock')

os.remove(lock_path)
