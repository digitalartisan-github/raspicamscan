# 2018(C) Ts Software
#   H.Teshima 
# # Reality Capture Batch script
#
# V1.3 2018/06/04
# (1) check camera position only 013,014
# (2) if not exits 013.jpg or 014.jpg , no retry
# (2) if not exits 013.xmp or 014.xmp , scale value is 100 fiexed
#
# ---- 2018/06/03 ---------
# ---- Emagency : no camera check
# ---- 
#
# V1.2 2018/5/2
# (1) Bug Fix
# V1.1 2018/4/27
# (1) scaleInfo value formatted {:.4f}
#
# V1.0
#  initila version

import xml.etree.ElementTree as ET
import os
import glob
import math
import copy
import sys
import numpy as np
import json
import collections as cl
#---------------------------------------------------------------
# center camera number and position for measurement
#
# 15
# 14
# 13
#-- ground --
#

#-- debug fixed value
# 1gouki

center_cam_list0 = ['091.xmp','092.xmp','093.xmp']
center_cam_dimension0 = {'091.xmp':300, '092.xmp':265, '093.xmp':265}

groud_offset0 = 8.0 # 

center_cam_list1 = ['091.xmp','092.xmp','093.xmp']
center_cam_dimension1 = {'091.xmp':300, '092.xmp':265 ,'093.xmp':265}

groud_offset1 = 8.0 # 

# 2gouki
center_cam_list2 = ['091.xmp','092.xmp','093.xmp']
center_cam_dimension2 = {'091.xmp':300, '092.xmp':265, '093.xmp':265}
groud_offset2 = 8.0

#---------------------------------------------------------------
# 3 dimensional coordinate
#  x,y,z
#

class Coord3:
  def __init__(self):
    self.x = 0.0
    self.y = 0.0
    self.z = 0.0

  def set(self,x,y,z):
    self.x = x
    self.y = y
    self.z = z

  def minset(self,x,y,z):
    if self.x > x:
      self.x = x
    if self.y > y:
      self.y = y
    if self.z > z:
      self.z = z
  
  def maxset(self,x,y,z):
    if self.x < x:
      self.x = x
    if self.y < y:
      self.y = y
    if self.z < z:
      self.z = z
  
  def distance(self,pt):
    d2 = (self.x - pt.x)*(self.x - pt.x)
    d2 += (self.y - pt.y)*(self.y - pt.y)
    d2 += (self.z - pt.z)*(self.z - pt.z)
    return math.sqrt(d2)

  def subtract(self,pt):
    self.x -= pt.x
    self.y -= pt.y
    self.z -= pt.z

  def vectorAngle2d(self,pt):
    v1 = np.array([self.x,self.y,0.0])
    v2 = np.array([pt.x,pt.y,0.0])
    dot_xy = np.dot(v1, v2)
    norm_1 = np.linalg.norm(v1)
    norm_2 = np.linalg.norm(v2)
    cos = dot_xy / (norm_1*norm_2)
    rad = np.arccos(cos)
    theta = rad * 180.0 / np.pi

    return theta

  def printMe(self):
    print('{},{},{}'.format(self.x,self.y,self.z))

#---------------------------------------------------------------
# 3 dimensional box
#  2 position min & max
#
class Box3:
  def __init__(self):
    self.count = 0
    self.min = Coord3()
    self.max = Coord3()

  def addPoint(self,point):
    self.count+=1
    x = float(point[0])
    y = float(point[1])
    z = float(point[2])
    if self.count==1:
      self.min.set(x,y,z)
      self.max.set(x,y,z)
      return
    self.min.minset(x,y,z)
    self.max.maxset(x,y,z)

  def getMin(self):
    return self.min

  def getMax(self):
    return self.max

  def size(self):
    length = []
    length.append(self.max.x-self.min.x)
    length.append(self.max.y-self.min.y)
    length.append(self.max.z-self.min.z)
    return length

  def size2(self):
    length = []
    length.append((self.max.x-self.min.x)/2.0)
    length.append((self.max.y-self.min.y)/2.0)
    length.append((self.max.z-self.min.z)/2.0)
    return length

  def center(self):
    pt=copy.copy(self.min)
    pt.x += (self.max.x-self.min.x)/2.0
    pt.y += (self.max.y-self.min.y)/2.0
    pt.z += (self.max.z-self.min.z)/2.0
    return pt

  def printMe(self):
    self.min.printMe()
    self.max.printMe()


#---------------------------------------------------------------
# Parse xmp file
#  1. Camera position
#  2. Camera direction(rotation)

class RealityCaptureXmp:
  ns = {'x': 'adobe:ns:meta/',
        'rdf': 'http://www.w3.org/1999/02/22-rdf-syntax-ns#',
        'xcr': 'http://www.capturingreality.com/ns/xcr/1.1#'}

  debug_sw = False
  def __init__(self,xmp_path):
    self.xmp_path = xmp_path

  def parseTemp(self):
    ret_code = 0
    self.pos =[]
    try:
      with open(self.xmp_path) as xmpfile:
        for rec in xmpfile:
          rec.strip()
          p = rec.find('Rotation')
          if p >= 0:
            # Rotation
            ret_code = ret_code + 1
            start_pos = rec.find('>') + 1
            item = rec[start_pos:-1]
            end_pos = item.find('<') - 1
            item = item[0:end_pos]
            self.rotation=item.split(' ')
          else:
            p = rec.find('Position')
            if p >= 0:
              # Position
              equ = rec.find('=')
              if equ >= 0:
                start_pos = rec.find('"') + 1
                item = rec[start_pos:-1]
                end_pos = item.find('"') - 1
                item = item[0:end_pos]
                self.pos=item.split(' ')
              else:
                ret_code = ret_code + 1
                start_pos = rec.find('>') + 1
                item = rec[start_pos:-1]
                end_pos = item.find('<') - 1
                item = item[0:end_pos]
                self.pos=item.split(' ')
    except Exception: 
      print ('I/O error')
      return -1
    
    return 0

  def parse(self):
    tree = ET.parse(self.xmp_path)
    root = tree.getroot()
    item=root.findall('rdf:RDF/rdf:Description/xcr:Position',RealityCaptureXmp.ns)
    if len(item)>0 :
      self.pos=item[0].text.split(' ')
    else:
      item=root.findall('rdf:RDF/rdf:Description',RealityCaptureXmp.ns)
      if len(item)>0 :
        self.pos=item[0].get('{http://www.capturingreality.com/ns/xcr/1.1#}Position').split(' ')
      else:
        self.pos =[]
      
    item1=root.findall('rdf:RDF/rdf:Description/xcr:Rotation',RealityCaptureXmp.ns)
    if len(item1)>0 :
      self.rotation=item1[0].text.split(' ')
    else:
      item=root.findall('rdf:RDF/rdf:Rotation',RealityCaptureXmp.ns)
      if ( len(item)>0 ):
        self.pos=item[0].get('{http://www.capturingreality.com/ns/xcr/1.1#}Rotation').split(' ')
      else:
        self.pos =[]
        
  def printMe(self):
    print('Position {}'.format(self.pos))

  def getPosition(self):
    return self.pos

  def getPositionCoord(self):
    coord = Coord3()
    coord.set(float(self.pos[0]),float(self.pos[1]),float(self.pos[2]))

    return coord


#---------------------------------------------------------------
# Camera Position from xmp file
#

class CameraPosition:
  def __init__(self,folder_path):
    self.folder_path = folder_path
    self.box = Box3()
    self.xmpMap = dict({})

  def removeAllXmp(self):
    target_file = os.path.join(self.folder_path,'*.xmp')
    xmp_files = glob.glob(target_file)
    for xmp_file in xmp_files:
      if ( os.path.exists(xmp_file)):
        os.remove(xmp_file)
    
  def traverse(self):
    # set zero to box
    self.box.addPoint([0,0,0])
    target_file = os.path.join(self.folder_path,'*.xmp')
    xmp_files = glob.glob(target_file)
    for xmp_file in xmp_files:
      xmp = RealityCaptureXmp(xmp_file)
      xmp.parse()
      self.box.addPoint(xmp.getPosition())
      file_elem = os.path.split(xmp_file)
      self.xmpMap[file_elem[1]] = xmp
      if RealityCaptureXmp.debug_sw == True:
        xmp.getPositionCoord().printMe()
    #
    # Check 1gou or 2gou

    self.center_cam_list = []
  
    # ----------
    if (self.xmpMap.keys() >= {'013.xmp', '014.xmp'}) != True:
      check_img_file = os.path.join(self.folder_path,'013.jpg')
      if os.path.exists(check_img_file) != True:
        return 101
      check_img_file = os.path.join(self.folder_path,'014.jpg')
      if os.path.exists(check_img_file) != True:
        return 102

      print ('No keys ... False')
      return -1
    # -----------

    result = 2
    if '013.xmp' not in self.xmpMap:
      return 101

    if '014.xmp' not in self.xmpMap:
      return 101

    p1 = self.xmpMap['013.xmp'].getPositionCoord()
    p2 = self.xmpMap['014.xmp'].getPositionCoord()
    check_dist = p1.distance(p2)
    if check_dist > 5.0:
      # 1gou
      if (self.xmpMap.keys() >= {'092.xmp', '059.xmp'}) != True:
        print ('No keys ... Flase')
        return -1
      p1 = self.xmpMap['092.xmp'].getPositionCoord()
      p2 = self.xmpMap['059.xmp'].getPositionCoord()
      check_dist = p1.distance(p2)
      if check_dist < 5.0:
        print(check_dist)
        # back entry
        print('0-gou')
        result = 0
        self.center_cam_list = center_cam_list0
        self.center_cam_dimension = center_cam_dimension0
        self.groud_offset = groud_offset0
      else:
        #side entry
        print('1-gou')
        result = 1
        self.center_cam_list = center_cam_list1
        self.center_cam_dimension = center_cam_dimension1
        self.groud_offset = groud_offset1
    else:
      # 2gou
      print('2-gou')
      self.center_cam_list = center_cam_list2
      self.center_cam_dimension = center_cam_dimension2
      self.groud_offset = groud_offset2
    return result

  def positionList(self):
    pos_list = []
    for xpm in self.xmp_list:
      pos = xpm.getPosition()
      pos_list.append(pos)
    return pos_list

  def getXmpMap(self):
    return self.xmpMap

  def getFrontDirecion(self,origin):
    if len(self.center_cam_list) == 0 :
      return 0.0
      
    key = self.center_cam_list[0]
    if key not in self.xmpMap:
      return 0.0
    v1 = self.xmpMap[key].getPositionCoord()
    v1.subtract(origin)
    v2 = Coord3()
    v2.set(0.0,-1.0,0.0)

    return v1.vectorAngle2d(v2)

  def boxCheck(self):
    # cehc box xy ratio
    size = self.box.size()
    if size[0] == 0:
      return False
    ratio = size[1] / size[0]
    print('#### Box ratio = {},height={}'.format(ratio,size[2]))

    if math.fabs(ratio - 1.0) > 0.1:
      return False

    return True

  def getGroundHeight(self):
    if len(self.center_cam_list) == 0 :
      return 0.0

    key = self.center_cam_list[0]
    if key not in self.xmpMap:
      return 0.0
    
    z0 = self.xmpMap[key].getPositionCoord().z
    d1 = self.center_cam_dimension[key]
    key = self.center_cam_list[1]
    if key not in self.xmpMap:
      return 0.0

    z1 = self.xmpMap[key].getPositionCoord().z
    d2 = self.center_cam_dimension[key]

    cd2 = z1 - z0
    cd1 = (d1*cd2)/d2

    cd_offset = (cd1*self.groud_offset)/d1
    print(cd_offset)
    
    return z0 - cd1 + cd_offset
  
  def getModelScale(self):
    if len(self.center_cam_list) == 0 :
      return 100.0

    key = self.center_cam_list[0]
    if key not in self.xmpMap:
      return 0.0
    
    z0 = self.xmpMap[key].getPositionCoord().z
    d1 = self.center_cam_dimension[key]
    key = self.center_cam_list[1]
    if key not in self.xmpMap:
      return 0.0
    
    z1 = self.xmpMap[key].getPositionCoord().z
    d2 = self.center_cam_dimension[key]

    scale = d2/(z1 - z0)

    return scale

  def checkUpDirection(self):
    if len(self.center_cam_list) == 0 :
      return True
    key = self.center_cam_list[0]
    if key not in self.xmpMap:
      return False
    
    x0 = self.xmpMap[key].getPositionCoord().x
    y0 = self.xmpMap[key].getPositionCoord().y
    z0 = self.xmpMap[key].getPositionCoord().z
    key = self.center_cam_list[1]
    if key not in self.xmpMap:
      return False
    x1 = self.xmpMap[key].getPositionCoord().x
    y1 = self.xmpMap[key].getPositionCoord().y
    z1 = self.xmpMap[key].getPositionCoord().z

    dx = math.fabs(x1 - x0)
    dy = math.fabs(y1 - y0)
    dz = math.fabs(z1 - z0)

    print('dx {} dy {} dz {}'.format(dx,dy,dz))
    if dz < dx:
      print('dx error')
      return False
    if dz < dy:
      print('dy error')
      return False
    if (z1 - z0) < 0.0:
      print('dz error')
      return False

    return True
      
  def createScaleInfo(self,out_path):
    try:
        f = open(out_path,'w')
    except:
      print('Cant file open {}',format(out_path))
      return False

    scale = self.getModelScale()
    center = self.box.center()
    angle = self.getFrontDirecion(center)

    scaleInfo = cl.OrderedDict()
    scaleInfo['scale_mm'] = '{:.4f}'.format(scale)
    scaleInfo['scale_cm'] = '{:.4f}'.format(scale/10.0)
    scaleInfo['scale_mm_1_12'] = '{:.4f}'.format(scale/12.0)
    scaleInfo['scale_mm_1_15'] = '{:.4f}'.format(scale/15.0)
    scaleInfo['scale_mm_1_20'] = '{:.4f}'.format(scale/20.0)
    scaleInfo['front_dir_angle'] = '{:.4f}'.format(angle)
    scaleInfo['front_dir_angle_meshmixer'] = '{:.4f}'.format(angle-90.0)

    #print("{}".format(json.dumps(scaleInfo,indent=4)))

    json.dump(scaleInfo,f,indent=4)

    f.close()

    return True

  def getFrontAngle(self):
    center = self.box.center()
    return self.getFrontDirecion(center)

#---------------------------------------------------------------
# Create model output parameter
#
class ModelOutputParameter:
  # File records
  morec1 = '<Model globalCoordinateSystem="+proj=geocent +ellps=WGS84 +no_defs" globalCoordinateSystemName="local:1 - Euclidean"'
  morec2 = '   exportCoordinateSystemType="0" transformToModel="1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1">'
  morec3 = '   <Header magic="5786959" version="1"/>'
  morec4 = '</Model>'
  morec5 = '<ModelExport exportBinary="1" exportInfoFile="1" exportVertices="1" exportVertexColors="0"'
  morec6 = ' exportVertexNormals="1" exportTriangles="1" exportTexturing="{}" meshColor="4294967295"'
  morec7 = ' tileType="0" exportTextureAlpha="0" exportToOneTexture="0" embeddTextures="1"'
  morec8 = ' oneTextureMaxSide="8192" oneTextureUsePow2TexSide="1" exportCoordinateSystemType="0"'
  morec9 = ' settingsAnchor="0 0 0" settingsScalex="1" settingsScaley="1" settingsScalez="1"'
  morec10= ' textureLayerIndex="0" textureWicContainerFormat="{19E4A5AA-5662-4FC5-A0C0-1758028E1057}"'
  morec11= ' textureWicPixelFormat="{6FDDC324-4E03-4BFE-B185-3D77768DC90C}" texturesFileType="jpg"'
  morec12= ' formatAndVersionUID="{}" exportModelByParts="0" exportRandomPartColor="0"'
  morec13= ' exportCameras="0" exportCamerasAsModelPart="0" numberAsciiFormatting="%.16e">'
  morec14= ' <Header magic="5786949" version="2"/>'
  morec15= '</ModelExport>'
  morec16= '<CalibrationExportSettings undistortImagesWicFormat="{1B7CFAF4-713F-473C-BBCD-6137425FAEAF}"'
  morec17= '   undistortImagesWicPixlFormat="{6FDDC324-4E03-4BFE-B185-3D77768DC90F}"'
  morec18= '   undistortDownscaleFactor="0" undistortNamingConvention="0" undistFitMode="0"'
  morec19= '   undistResMode="0" undistPrincipalMode="0" undistCutOut="0" undistMaxPixels="0"'
  morec20= '   undistBackColor="0" undistortCustomWidth="0" undistortCustomHeight="0"'
  morec21= '   undistortCalibration="0" undistortImagesExtension="png" undistortImageNameSuffix=""'
  morec22= '   exportUndistorted="0" exportDisabled="0"/>'


  def createParameterFile(out_path,texture,format):

    #---------------
    # create file
    try:
       f = open(out_path,'w')
    except:
      print('Cant file open {}',format(out_path))
      return False

    exportFormat = "obj 000"
    if format.upper() == 'FBX':
      exportFormat = "fbx 000 FBX201900"

    if texture:
      textureFlag = 1
    else:
      textureFlag = 0
   
    
    #print(' create output parameter file {}\n'.format(out_path))
    #print(' output parameter {} {}\n'.format(maxSize,textureFlag))

    f.write(ModelOutputParameter.morec1 + '\n')
    f.write(ModelOutputParameter.morec2 + '\n')
    f.write(ModelOutputParameter.morec3 + '\n')
    f.write(ModelOutputParameter.morec4 + '\n')
    f.write(ModelOutputParameter.morec5 + '\n')
    f.write(ModelOutputParameter.morec6.format(textureFlag) + '\n')
    f.write(ModelOutputParameter.morec7 + '\n')
    f.write(ModelOutputParameter.morec8 + '\n')
    f.write(ModelOutputParameter.morec9 + '\n')
    f.write(ModelOutputParameter.morec10 + '\n')
    f.write(ModelOutputParameter.morec11 + '\n')
    f.write(ModelOutputParameter.morec12.format(exportFormat) + '\n')
    f.write(ModelOutputParameter.morec13 + '\n')
    f.write(ModelOutputParameter.morec14 + '\n')
    f.write(ModelOutputParameter.morec15 + '\n')
    f.write(ModelOutputParameter.morec16 + '\n')
    f.write(ModelOutputParameter.morec17 + '\n')
    f.write(ModelOutputParameter.morec18 + '\n')
    f.write(ModelOutputParameter.morec19 + '\n')
    f.write(ModelOutputParameter.morec20 + '\n')
    f.write(ModelOutputParameter.morec21 + '\n')
    f.write(ModelOutputParameter.morec22 + '\n')
   
    f.close()

    return True


#---------------------------------------------------------------
# Create Reconstruction RegionBox
#
class ReconstructRegion:
  # File records  
  rcbox1 = '<ReconstructionRegion globalCoordinateSystem="+proj=geocent +ellps=WGS84 +no_defs" globalCoordinateSystemName="local:1 - Euclidean"'
  rcbox2 = '   isGeoreferenced="{}" isLatLon="0" yawPitchRoll="0 -0 {}" widthHeightDepth="{} {} {}">'
  rcbox3 = '  <Header magic="5395016" version="2"/>'
  rcbox4 = '  <CentreEuclid>'
  rcbox5 = '    <centre>{} {} {}</centre>'
  rcbox6 = '  </CentreEuclid>'
  rcbox7 = '  <Residual R="1 0 0 0 1 0 0 0 1" t="0 0 0" s="1"/>'
  rcbox8 = '</ReconstructionRegion>'


  # static method : createFile
  #  camPos : CameraPosition instance
  #  out_path : file path of ReconstuctRegion
  def createFile(camPos,out_path,out_path1,bottom):
    # Calcrate size
    size = camPos.box.size()
    if RealityCaptureXmp.debug_sw == True:
      print(size)
    if size[0] > size[1]:
      r = size[0]/2
    else:
      r = size[1]/2
    inscribed = r * math.sqrt(2) * 0.9

    size[0] = size[1] = inscribed

    # Calcrate center
    center = camPos.box.center()
    if bottom == 0:
      center.z = size[2]/2.0
      compensation =  camPos.getGroundHeight()
      center.z += compensation
    else:
      size[2] = size[2] - bottom
      center.z = size[2]/2.0 + bottom

    angle = camPos.getFrontDirecion(center)

    #---------------
    # create file
    try:
       f = open(out_path,'w')
    except:
      print('Cant file open {}',format(out_path))
      return False

    f.write(ReconstructRegion.rcbox1 + '\n')
    f.write(ReconstructRegion.rcbox2.format(0,angle,size[0],size[1],size[2])+'\n')
    f.write(ReconstructRegion.rcbox3 + '\n')
    f.write(ReconstructRegion.rcbox4 + '\n')
    f.write(ReconstructRegion.rcbox5.format(center.x,center.y,center.z) + '\n')
    f.write(ReconstructRegion.rcbox6 + '\n')
    f.write(ReconstructRegion.rcbox7 + '\n')
    f.write(ReconstructRegion.rcbox8 + '\n')
    
    f.close()

    #---------------
    # create file
    try:
       f = open(out_path1,'w')
    except:
      print('Cant file open {}',format(out_path))
      return False

    f.write(ReconstructRegion.rcbox1 + '\n')
    f.write(ReconstructRegion.rcbox2.format(1,angle,size[0],size[1],size[2])+'\n')
    f.write(ReconstructRegion.rcbox3 + '\n')
    f.write(ReconstructRegion.rcbox4 + '\n')
    f.write(ReconstructRegion.rcbox5.format(center.x,center.y,center.z) + '\n')
    f.write(ReconstructRegion.rcbox6 + '\n')
    f.write(ReconstructRegion.rcbox7 + '\n')
    f.write(ReconstructRegion.rcbox8 + '\n')
    
    f.close()

    return True

  #
  # ---
  #
  def RewriteFile(camPos,file_path):
    size = camPos.box.size()
    if RealityCaptureXmp.debug_sw == True:
      print(size)
    if size[0] > size[1]:
      r = size[0]/2
    else:
      r = size[1]/2
    inscribed = r * math.sqrt(2) * 0.75

    size[0] = size[1] = inscribed

    # Calcrate center
    center = camPos.box.center()
    center.z = size[2]/2.0

    angle = camPos.getFrontDirecion(center)
    if RealityCaptureXmp.debug_sw == True:
      print(angle)

    compensation =  camPos.getGroundHeight()
    print (compensation)
    center.z += compensation

    # load box file
    tree = ET.parse(file_path)
    root = tree.getroot()

    root.attrib['yawPitchRoll'] = '0 0 {}'.format(angle)
    root.attrib['widthHeightDepth'] = size
    item=root.findall('CentreEuclid')
    item[0].text = '{} {} {}'.format(center.x,center.y,center.z)

    temp_dir = os.path.split(file_path)
    temp_xml = os.path.join(temp_dir[0],'temp_region.xml')
    tree.write(temp_xml)
    
    os.unlink(file_path)
    os.rename(temp_xml,file_path)

    return True

