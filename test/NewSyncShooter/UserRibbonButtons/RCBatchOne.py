# 2018(C) Ts Software
#   H.Teshima 
# # Reality Capture Batch script
#
import traceback

try:
  import RCBatch
  import sys

  if __name__ == '__main__':
    argvs = sys.argv  # 
    argc = len(argvs) # 
    if ( argc > 2):
      source_image_path=argvs[1]
      target_path=argvs[2]
      cut_stand=argvs[3]

      if RCBatch.BatchProc.IsRealityCaptureRunning():
        print("RealityCapture is running")
      elif  RCBatch.BatchProc.getLock():
        RCBatch.BatchProc.setImageFolder(source_image_path)
        RCBatch.BatchProc.setTopFolder(target_path)

        main_proc = RCBatch.BatchProc()
        main_proc.construct3D('',cut_stand,'overwrite')

        RCBatch.BatchProc.releaseLock()
      else:
        print("another bactch excuted")

except:
    traceback.print_exc() 
    a = input()