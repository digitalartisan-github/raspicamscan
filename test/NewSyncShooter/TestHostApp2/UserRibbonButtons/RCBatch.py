# 2018(C) Ts Software
#   H.Teshima 
# # Reality Capture Batch script
#
# +--source_image_path-+- date=time(photo_number)\       source image
#                      +- date=time(photo_number1)\
#                      +- date=time(photo_number2)\
#
# +--output_model_path -+- project\ -+- photo_number.rcproj         RC project
#                       |
#                       +- compornent\        temporary RC compornent
#                       |
#                       +- model\ -+- photo_number\ -+- photo_number.obj        3d model
#                       |          |                  +- photo_number.mtl        materail
#                       |          |                  +- photo_number_u1_v1.jpg  texture
#                       |          |                  +- scaleInfo.txt
#                       |          |
#                       |          +- photo_number1\ -+- photo_number1.obj        3d model
#                       |
#                       +- log\ --- rclog-date-time.log
#                       |
#                       +- 3d-image\ -+- photo_number-f.png
#                                     +- photo_number-r.png
#
# V1.6.1 2
# (1) Change Alignment sequence
#
# V1.6 2018/5/2
# (1) Bug Fix
# V1.5 2018/4/28
# (1) Bug.fix character space included path name
# (2) No photonumber , 'no-name'
# (3) Create 3D image (PNG)
#
# V1.4 2018/4/16
# (1) (modify) split input image folder and output result folder
# (2) (add) batch process single image folder
# (3) (add) lock file for exclusive batch process
# (4) (add) Check & Cut PetStand 
#
# V1.3 2018/3/28
#  (1) Align retry if fail
#  (2) Automatic calclate of reconstruct-region
#  (3) Remove Garbage object
#
# V1.2 2018/2/27
#   (1) Correspond to RC Version 1.0.3.3939 
#        Change exportCompornent command parameter file to folder
# V1.1 2018/02/19
#  (1) polygon simplfy ,step by step
#  (2) step time output logging
#
# V1.0
#  initila version

import subprocess
import sys
import os
import re
import subprocess
import datetime
import time
from datetime import tzinfo, timedelta, datetime ,date
import RealityCaptureXmp
import math
import tempfile
import io
import re
import collections as cl
import json


#--------------------------------------------------------------------------------------------
#--------------------------------------------------------------------------------------------
# Logging class
#
class ProcLogger:

  def __init__(self,log_path):
    self.start_log = datetime.today()

    logname = 'RCB{}.log'.format(time.strftime('%Y-%m-%d-%H%M%S',time.localtime()))
    log_file_path = os.path.join(log_path,logname)
    self.logf = open(log_file_path,'w')
    self.log_start_date = datetime.today()
    self.logf.writelines( 'Start logging : ' + self.log_start_date.ctime())
    self.logf.writelines('\n')
    self.logf.flush()

  def logging(self,log_msg):
    now = datetime.today()
    self.logf.writelines(log_msg + ' : ' + now.ctime())
    self.logf.writelines('\n')
    self.logf.flush()

  def proc_start(self,log_msg):
    self.proc_start_time = datetime.today()
    self.logf.writelines( log_msg + ' : ' + self.proc_start_time.ctime()) 
    self.logf.writelines('\n')
    self.logf.flush()

  def time_calc_start(self):
    self.tc_start_time = datetime.today()

  def time_calc_end(self):
    now = datetime.today()
    time_span = now - self.tc_start_time
    return time_span

  def proc_end(self,log_msg):
    now = datetime.today()
    time_span = now - self.proc_start_time
    sec = ' process time(' + str(time_span) + ')'
    self.logf.writelines(log_msg + ' : ' + now.ctime()+ sec )
    self.logf.writelines('\n')
    self.logf.flush()

  def logging_end(self):
    now = datetime.today()
    time_span = now - self.start_log    
    total = ' Total time(' + str(time_span) +')'
    self.logf.writelines('End logging' + ' : ' + now.ctime()+ total )
    self.logf.writelines('\n')
    self.logf.flush()

#--------------------------------------------------------------------------------------------
#--------------------------------------------------------------------------------------------
# Interrupt process
#
class BatchProcInterrput(Exception):
  def __init__(self,msg):
    print (msg)

#--------------------------------------------------------------------------------------------
#--------------------------------------------------------------------------------------------
# Batch process class
#
class BatchProc:
  noname_no = 0
  top_folder=''
  image_folder=''
  lock_status=False

  export_file_format = 'obj'

  # Process control
  def IsRealityCaptureRunning():
    cmd = 'tasklist /fi "imagename eq RealityCapture.exe" /nh'
    proc = subprocess.Popen(cmd, shell=True, stdout=subprocess.PIPE)
    for line in proc.stdout:
      line_str = str(line)
      result = re.findall('RealityCapture',line_str)
      if result != []:
        return True
    return False

  def getLock():
    lock_file = BatchProc.getLockFilePath()
    if os.path.exists(lock_file) == True:
      return False  # fail lock
    
    lock_f = io.FileIO(lock_file,'w')  # create lock file
    lock_f.close()

    BatchProc.lock_status = True
    return True

  def releaseLock():
    if BatchProc.lock_status == True:
      os.remove(BatchProc.getLockFilePath())
    BatchProc.lock_status = False

  def getLockFilePath():
    dir = tempfile.gettempdir()
    return os.path.join(dir,'_rc_bacth_lock')

  def checkInterrupted():
    if os.path.exists(BatchProc.interrpurt_mark):
      os.remove(BatchProc.interrpurt_mark)
      raise BatchProcInterrput('Interrput')

  # Settings
  def loadSettings(setting_file):
    try:
      f = open(setting_file ,'r')
    except IOError:
      print ('{} cannot be opened.use default'.format(setting_file_path))
    else:
      jval = json.load(f)
      BatchProc.RCexe = jval["RealityCapture"]
     
      BatchProc.normal_first_polygpn_limit = jval["NormalDetail"]["first_polygon_limit"]
      BatchProc.normal_final_polygpn_limit = jval["NormalDetail"]["final_polygon_limit"]
      
      BatchProc.stand_height = jval["stand_height"]
      BatchProc.stand_thickness = jval["stand_thickness"]

      BatchProc.export_file_format = jval["export_file_format"]

      f.close()
  
  dir = tempfile.gettempdir()
  interrpurt_mark = os.path.join(dir,'_rc_interrupt_mark')

  # Input - output folder    
  def setImageFolder(top):
    BatchProc.image_folder=top
    if  BatchProc.image_folder[:-1] == '\\':
      BatchProc.image_folder = BatchProc.image_folder[:-1]
    print(BatchProc.image_folder)

  def setTopFolder(top):
    BatchProc.top_folder=top
    if  BatchProc.top_folder[:-1] == '\\':
      BatchProc.top_folder=BatchProc.top_folder[:-1]
  
  # Instance
  def __init__(self):
    # setup loging
    log_path = os.path.join(BatchProc.top_folder,'log')
    if os.path.exists(log_path)!=True:
      os.mkdir(log_path)
    self.mylog = ProcLogger(log_path)
    self.mylog.logging('Create 3D model start')
    #
    # load settings
    setting_file_path = os.path.join( os.getcwd(),'BatchSettings.json')
    BatchProc.loadSettings(setting_file_path);

  # Photonumber from folder name
  def splitPhotoNumber(self,image_folder_name):
    first = re.split('\(',image_folder_name)
    second = re.split('\)',first[1])
    pnumber = second[0]
    if len(pnumber)==0:
      pnumber = 'no-name' + str(BatchProc.noname_no)
      BatchProc.noname_no += 1
      
    return pnumber


  # 3D
  def construct3D(self,target_flag,cut_mode,overwrite_mode):
    ##src_image=os.path.join(BatchProc.image_folder,'image')
    src_image=BatchProc.image_folder
    project = os.path.join(BatchProc.top_folder,'project')
    # check exists project folder
    if os.path.exists(project)!=True:
      os.mkdir(project)

    compornent = os.path.join(BatchProc.top_folder,'compornent')
    # check exists compornent folder
    if os.path.exists(compornent)!=True:
      os.mkdir(compornent)

    model = os.path.join(BatchProc.top_folder,'model')
    # check exists model folder
    if os.path.exists(model)!=True:
      os.mkdir(model)

    # Region BOX
    box_path0 = os.path.join(BatchProc.top_folder,'calc0.rcbox')
    box_path1 = os.path.join(BatchProc.top_folder,'calc1.rcbox')

    # Mesh output parameter file
    model_config = os.path.join(BatchProc.top_folder,'outParams.xml')
    check_stop = os.path.join(BatchProc.top_folder,'_stop.txt')

    # Garbage remover
    garbage_remover = os.path.join( os.getcwd(),'garbageRemover.exe')

    # pet stand checker
    pet_stand_checker = os.path.join( os.getcwd(),'petStandCheck.exe')

    # 3d image(PNG)
    image_3d_creator = os.path.join( os.getcwd(),'ObjPreviewCreator.exe')
    image_3d = os.path.join(BatchProc.top_folder,'3d_image')
    if os.path.exists(image_3d)!=True:
      os.mkdir(image_3d)


    BatchProc.checkInterrupted()
    try:
      if target_flag == 'all':
        count = 0
        self.proc_num = 0
        image_list = os.listdir(src_image)
        for image_unit in image_list:
          if os.path.exists(check_stop)==True:
            print('!! Process canceled')
            break

          target_image = os.path.join(src_image,image_unit)
          if os.path.isdir(target_image)!=True:
            continue

          photo_number = self.splitPhotoNumber(image_unit)

          count+=1
          print('('+photo_number+')' + ' ' + str(count))

          RC=RealityCaputre(self.mylog)
          RC.setOption(cut_mode,overwrite_mode)
          RC.setup(photo_number,target_image,project,compornent,model,model_config,garbage_remover,pet_stand_checker)
          RC.setRecostructRegionPath(box_path0,box_path1)
          RC.set3dimageFolder(image_3d,image_3d_creator)
          RC.ExecuteRC()
      else:
        image_unit = BatchProc.image_folder
        photo_number = self.splitPhotoNumber(image_unit)
        print('('+photo_number+')' )

        target_image = os.path.join(src_image,image_unit)
        RC=RealityCaputre(self.mylog)
        RC.setOption(cut_mode,overwrite_mode)
        RC.setup(photo_number,target_image,project,compornent,model,model_config,garbage_remover,pet_stand_checker)
        RC.setRecostructRegionPath(box_path0,box_path1)
        RC.set3dimageFolder(image_3d,image_3d_creator)
        RC.ExecuteRC()
    except BatchProcInterrput:
      print('stopped')


    self.mylog.logging_end()


#--------------------------------------------------------------------------------------------
#--------------------------------------------------------------------------------------------
#
# Create 3D Model form source image
#
class RealityCaputre:
  def __init__(self,mylog):
    self.mylog = mylog
    self.remove_garbage = True
    self.pet_stand_cut = 'auto'   # 'auto' , 'yes'  , 'no' 
    self.image_3d_creator =''
    self.image_3d_folder = ''

  def setup(self,photo_number,source_image,project,tmp_compornent,model,model_config,garbage_remover,pet_stand_checker):
    self.photo_number = photo_number
    self.source_image_path = source_image
    self.output_project = project
    self.tmp_compornent = tmp_compornent
    self.model_path = model
    self.model_config = model_config
    self.model_info_path = os.path.join(model,photo_number)
    self.model_scale_path = os.path.join(self.model_info_path,'scaleInfo.json')
    self.pet_stand_path = os.path.join(self.model_info_path,'petStand.json')
    self.pet_stand_info_path = os.path.join(self.model_info_path,'petStandInfo.json')
    self.garbage_remover = garbage_remover
    self.pet_stand_checker = pet_stand_checker

  def printMe(self):
    print(self.source_image_path)
    print(self.output_project)

  def setRecostructRegionPath(self,box_path0,box_path1):
    self.box_path0 = box_path0
    self.box_path1 = box_path1
  
  def set3dimageFolder(self,folder_path,exe_path):
    self.image_3d_folder = folder_path
    self.image_3d_creator = exe_path

  def setOption(self,cut_mode,overwrite_mode):
    self.cut_mode=cut_mode
    self.overwrite_mode=overwrite_mode

  def aligneImage(self,count,project_path):
    self.mylog.logging(' - step 1 load image')
    BatchProc.checkInterrupted()

    self.mylog.time_calc_start()

    sfmMaxFeaturesPerImage = 40000
    if count == 0:
      DetectorSensitivity = 'Medium'
    else:
      DetectorSensitivity = 'High'
      sfmMaxFeaturesPerImage += (count-1)*20000
    
    print('DetectorSensitivity=' + DetectorSensitivity)
    print('sfmMaxFeaturesPerImage=' + str(sfmMaxFeaturesPerImage))

    BatchProc.checkInterrupted()
    cl= '{} -set "sfmMaxFeaturesPerImage={}"'\
        ' -set "sfmDetectorSensitivity={}"'\
        ' -addFolder "{}"'\
        ' -draft  -save "{}"  -quit'.format(BatchProc.RCexe,sfmMaxFeaturesPerImage,DetectorSensitivity,self.source_image_path,project_path)
    print(cl)
    res = subprocess.run(cl)

    step_time = self.mylog.time_calc_end()
    self.mylog.logging('       load image time ({})'.format(str(step_time)))

    #
    # 2 : align and export compornent
    #
    self.mylog.logging(' - step 2 aligin')
    self.mylog.time_calc_start()

    BatchProc.checkInterrupted()
    tmp_compornent_path = self.tmp_compornent
    cl= '{} -set "sfmMaxFeaturesPerImage={}"'\
         '  -set "sfmDetectorSensitivity={}"'\
         '  -load "{}"'\
         '  -save "{}"'\
         '  -quit'.format(BatchProc.RCexe,sfmMaxFeaturesPerImage,DetectorSensitivity,project_path,project_path)
    print(cl)
    res = subprocess.run(cl)

    BatchProc.checkInterrupted()
    tmp_compornent_path = self.tmp_compornent
    cl= '{} -load "{}"'\
         '  -align -selectMaximalComponent -exportComponent  "{}" -exportXmp'\
         '  -quit'.format(BatchProc.RCexe,project_path,tmp_compornent_path)
    print(cl)
    res = subprocess.run(cl)
    compornent_list = os.listdir(self.tmp_compornent)
    if len(compornent_list) == 0:
      # ERROR
      self.mylog.logging(' - step 2 compornent error !!')
      return -1

    step_time = self.mylog.time_calc_end()
    self.mylog.logging('       aligin time ({})'.format(str(step_time)))


    return compornent_list

  def createPetStandParameter(self,scale,model_path,file_path):
    try:
        f = open(file_path,'w')
    except:
      print('Cant file open {}',format(file_path))
      return False

    standInfo = cl.OrderedDict()
    standInfo['scale_mm'] = scale
    standInfo['mesh_file'] = model_path
    standInfo['stand_height'] = BatchProc.stand_height
    standInfo['stand_thickness'] = BatchProc.stand_thickness

    print("{}".format(json.dumps(standInfo,indent=4)))

    json.dump(standInfo,f,indent=4)

    f.close()
    return True

  #-------------------------------------
  # execure RealityCapture
  #
  def ExecuteRC(self):   
    #
    # 0: Initial setup
    #  new camera object instance
    #  clean up files
    #
    model_dir_path = os.path.join(self.model_path,self.photo_number)
    if os.path.exists(model_dir_path)!=True:
      os.mkdir(model_dir_path)
    model_path = os.path.join(model_dir_path,self.photo_number+'.obj')
    if self.overwrite_mode == 'skip':
      print("check " + model_path)
      if os.path.exists(model_path) == True:
        # skip
        self.mylog.logging(' ----- skip ------')
        return True

    cam = RealityCaptureXmp.CameraPosition(self.source_image_path)

    camera_db = os.path.join(self.source_image_path,'crmeta.db')
    if ( os.path.exists(camera_db)):
      os.remove(camera_db)

    #
    # 1 : load image and align(draft) and align , if direction is wrong try align more 5 times 
    #
    self.mylog.proc_start ('- (' + self.photo_number + ')')

    project_file = self.photo_number + '.rcproj'
    project_path = os.path.join(self.output_project,project_file)

    aligin = False
    for i in range(7):
      cam.removeAllXmp()
      compornent_list = self.aligneImage(i,project_path)
      cam = RealityCaptureXmp.CameraPosition(self.source_image_path)
      check_rig = cam.traverse()
      if check_rig >= 0:
        if cam.checkUpDirection() == True:
          # center check
          if cam.boxCheck() == True:
            aligin = True      
            break
          if check_rig == 1:
            BatchProc.stand_height = BatchProc.stand_height - 20

      # Retry
      self.mylog.logging(' ++ retry align')
      cam.removeAllXmp()
      camera_db = os.path.join(self.source_image_path,'crmeta.db')
      if ( os.path.exists(camera_db)):
        os.remove(camera_db)      
  
    #
    # 2.1 : Calculate Region Box
    #
    RealityCaptureXmp.ReconstructRegion.createFile(cam,self.box_path0,self.box_path1,0)

    # 
    # 3 : import compornent ,import area 
    # 
    self.mylog.logging(' - step 3 reconstruction')
    self.mylog.time_calc_start()

    componrnt_path = os.path.join(self.tmp_compornent, compornent_list[0])
    componrnt_path_remove = os.path.join(self.tmp_compornent, compornent_list[0])
    #cl= BatchProc.RCexe +' -importComponent ' + componrnt_path + ' -setReconstructionRegion ' + self.box_path0 + ' -save ' + project_path + ' -quit'
    #cl= '{} -importComponent "{}" -setReconstructionRegion "{}" -save "{}"'\
    #    ' -quit'.format(BatchProc.RCexe,componrnt_path,self.box_path0,project_path)
    cl= '{} -importComponent "{}" -setReconstructionRegion "{}"  -save "{}" -quit'.format(BatchProc.RCexe,componrnt_path,self.box_path0,project_path)
    print(cl)
    res = subprocess.run(cl)
    os.remove(componrnt_path_remove)

    # 3.1 re-contruction
    # Calculate 3D mesh in normal quality
    # Simplify 1st
    BatchProc.checkInterrupted()
    cl= '{} -load "{}" '\
        ' -mvs -simplify {}'\
        ' -save "{}" -quit'\
        ' '.format(BatchProc.RCexe,project_path,BatchProc.normal_first_polygpn_limit,project_path)
    print(cl)
    res = subprocess.run(cl)

    # Create model configration file
    RealityCaptureXmp.ModelOutputParameter.createParameterFile(self.model_config,False,'obj')
    cam.createScaleInfo(self.model_scale_path)

    BatchProc.checkInterrupted()
    cl= '{} -load "{}" '\
        ' -exportModel "Model 2" "{}" "{}" -quit'.format(BatchProc.RCexe,project_path,model_path,self.model_config)
    res = subprocess.run(cl)

    step_time = self.mylog.time_calc_end()
    self.mylog.logging('       reconstruction time ({})'.format(str(step_time)))

    # 
    # 3.2 : Garbage remove
    #
    
    self.mylog.logging(' - step 3.1 garbage remove')
    self.mylog.time_calc_start()

    BatchProc.checkInterrupted()
    if self.remove_garbage:
      tmp_model_path = model_path + '.obj'
      cl= '{}  "{}"  "{}"'.format(self.garbage_remover,model_path,tmp_model_path)
      res = subprocess.run(cl)

      # check tmp_model_path ...
      if os.path.exists(tmp_model_path):
        os.remove(model_path)
        os.rename(tmp_model_path,model_path)

      step_time = self.mylog.time_calc_end()
    else:
      self.mylog.logging('       garbage remove skip')

    self.mylog.logging('       garbage remove time ({})'.format(str(step_time)))

    # 3.2.1 : check pet stand
    if self.cut_mode != 'no':
      # create parameter file
      scale = cam.getModelScale()
      self.createPetStandParameter( scale,model_path,self.pet_stand_path)
      cl= '{}  "{}"  "{}"'.format(self.pet_stand_checker,self.pet_stand_path,self.pet_stand_info_path)
      print(cl)
      res = subprocess.run(cl)
      if os.path.exists(self.pet_stand_info_path) == True:
        # check result
        f = open(self.pet_stand_info_path ,'r')
        d = json.load(f)
        f.close()
        if  d['stand_flag'] == 'True':
          # -- PET STAND --
          # calc reconstruct reigion
          bottom = d['stand_height']
          RealityCaptureXmp.ReconstructRegion.createFile(cam,self.box_path0,self.box_path1,bottom)

          # import & re-construct
          cl= '{} -load "{}" -setReconstructionRegion "{}"'\
              ' -mvs -simplify {} -renameModel "petModel" '\
              ' -save "{}" -quit'\
              ' '.format(BatchProc.RCexe,project_path,self.box_path0,BatchProc.normal_first_polygon_limit,project_path)
          res = subprocess.run(cl)

          # export mesh
          cl= '{} -load "{}" '\
              ' -exportModel "petModel" "{}" "{}" -quit'.format(BatchProc.RCexe,project_path,model_path,self.model_config)
          res = subprocess.run(cl)
          # remove garbage
          if self.remove_garbage:
            tmp_model_path = model_path + '.obj'
            cl= '{}  "{}"  "{}"'.format(self.garbage_remover,model_path,tmp_model_path)
            res = subprocess.run(cl)

            # check tmp_model_path ...
            if os.path.exists(tmp_model_path):
              os.remove(model_path)
              os.rename(tmp_model_path,model_path)
    # 
    # 3.3 : import clean model
    #
    BatchProc.checkInterrupted()
    cl=  '{} -load "{}" -importModel "{}" -simplify {}'\
         ' -save "{}"'\
         ' -quit'.format(BatchProc.RCexe,project_path,model_path,BatchProc.normal_final_polygpn_limit,project_path)
    res = subprocess.run(cl)

    #
    # 4 : smooth and texture ,model named 'FinalModel'
    #
    self.mylog.logging(' - step 4 smooth and texture')
    self.mylog.time_calc_start()

    BatchProc.checkInterrupted()
    cl= '{} -load "{}" -smooth  -calculateTexture -renameModel'\
        ' "FinalModel" -save "{}" -quit'.format(BatchProc.RCexe,project_path,project_path)
    res = subprocess.run(cl)
    self.mylog.logging('       smooth and texture ({})'.format(str(step_time)))

    # 5 : export model
    self.mylog.logging(' - step 5 export model')
    self.mylog.time_calc_start()

    # output model named 'FinalModel'
    RealityCaptureXmp.ModelOutputParameter.createParameterFile(self.model_config,True,BatchProc.export_file_format)

    BatchProc.checkInterrupted()
    cl= '{} -load "{}"  -exportModel "FinalModel" "{}" "{}" '\
        ' -quit'.format(BatchProc.RCexe,project_path,model_path,self.model_config)
    print(cl)
    res = subprocess.run(cl)
    self.mylog.logging('       export model ({})'.format(str(step_time)))
    
#    if len(self.image_3d_folder) != 0:
#      angle = cam.getFrontAngle()
#      cl = '{} {} {} {}'.format(self.image_3d_creator,model_path,self.image_3d_folder,angle)
#      print(cl)
#      res = subprocess.run(cl)


    # Final : clean up
    if os.path.exists(self.box_path0) == True:
      os.remove(self.box_path0)

    if os.path.exists(self.box_path1) == True:
      os.remove(self.box_path1)

    if os.path.exists(self.model_config) == True:
      os.remove(self.model_config)

    if BatchProc.export_file_format.upper() == 'FBX':
      # obj
      if os.path.exists(model_path) == True:
        os.remove(model_path)
      # rcinfo
      tmp_rcinfo_path = model_path + '.rcinfo'
      if os.path.exists(tmp_rcinfo_path) == True:
        os.remove(tmp_rcinfo_path)
      #mtl
      tmp_mtl_path = os.path.splitext(model_path)[0] + '.mtl'
      if os.path.exists(tmp_mtl_path) == True:
        os.remove(tmp_mtl_path)
      # move to horizontal plane
      target_fbx = os.path.splitext(model_path)[0] + '.fbx'
      fxb_editor = os.path.join( os.getcwd(),'transformData.exe')
      cl= '{} "{}"'.format(fxb_editor,target_fbx)
      print(cl)
      res = subprocess.run(cl)
      self.mylog.logging('       fbx edit ({})'.format(str(step_time)))



    self.mylog.proc_end ('- (' + self.photo_number + ')')

    return 0

