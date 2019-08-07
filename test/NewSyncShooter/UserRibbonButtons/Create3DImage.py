# 2018(C) Ts Software
# Reality Capture Batch script
#
# +--source_path-+- image\ -+- date=time(photo_number)\       source image


import os
import argparse
import re
import subprocess


exe_path = 'ObjPreviewCreator.exe'

class Create3DImage:
  top_folder=''

  def setTopFolder(top):
    Create3DImage.top_folder=top

  def splitPhotoNumber(self,image_folder_name):
    first = re.split('\(',image_folder_name)
    second = re.split('\)',first[1])
    return second[0]

  def createImage(self,photo_number):
    print ('create '+photo_number)
    model_dir =  os.path.join(self.src_model,photo_number)
    obj_file = os.path.join(model_dir,photo_number)
    obj_file += '.obj'
    cl= exe_path +' ' + obj_file + ' ' + self.out_path
    res = subprocess.run(cl)

  def exec(self):
    self.out_path=os.path.join(Create3DImage.top_folder,'3d-image')
    self.src_model=os.path.join(Create3DImage.top_folder,'model')

    image_list = os.listdir(self.src_model)
    for photo_number in image_list:
      self.createImage(photo_number)



#-----------------------------------
# Main Proc
#

# command line args
parser = argparse.ArgumentParser(description='Data folder')
parser.add_argument('path_data_src', \
        action='store', \
        nargs=None, \
        const=None, \
        default=None, \
        type=str, \
        choices=None, \
        help='Folder path where your taken photo files are located.', \
        metavar=None)
args=parser.parse_args()
source_path=args.path_data_src

main_proc = Create3DImage()
Create3DImage.setTopFolder(source_path)

main_proc.exec()



