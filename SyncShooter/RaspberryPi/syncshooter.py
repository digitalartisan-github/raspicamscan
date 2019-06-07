#!/usr/bin/python3
#
# V1.71 No play sound
# v1.7 add camera settings (wb ,shutter_speed)
# v1.6 use multi port for image sending
# v1.4 support JPEG
#

import sys
sys.path.append("./piexif")
import piexif

import json

import socket
import struct
import fcntl

import threading
import logging

import os
import time
import subprocess
from subprocess import Popen


from io import BytesIO
from time import sleep
from picamera import PiCamera

#----------------------------------
logging.basicConfig(level=logging.DEBUG,
                    format='(%(threadName)-10s) %(message)s',
                    )


json_init_file = '/home/pi/picamscan/cam_def.json'

#=================================
class CameraControl :
  #-- Common variables
  sem = threading.Semaphore(1)

  camera = PiCamera()
  params = dict(max_resol=(3280, 2464),
                preview_resol=(1640, 1232),
                drc_strength='high',
                awb_mode='off',
                wb_rg=1.5,
                wb_gb=1.5,
                brightness=50,
                jpeg_quality = 100,
                Orientation=1)

  image_stream = BytesIO()
  image_stream.close()
  preview_stream = BytesIO()
  preview_stream.close()

  #-- define method
  def shutterSound(self):
    #cmd = '/home/pi/picamscan/kasya.py'
    #proc = Popen( cmd .strip().split(" ") )
    return

  def lock(self):
    logging.debug('+++ enter lock')
    CameraControl.sem.acquire()
    logging.debug('+++ exit lock')

  def unlock(self):
    logging.debug('--- enter unlock')
    CameraControl.sem.release()
    logging.debug('--- exit unlock')
    return

  def set_orientation(self,dir):
    CameraControl.params['Orientation'] = dir

  def get_orientation(self):
    return CameraControl.params['Orientation']

  def param_apply(self):
    CameraControl.camera.awb_mode = CameraControl.params['awb_mode']
    if CameraControl.params['Shutter_speed_mode'] == 'auto':
      CameraControl.camera.shutter_speed = 0
    else:
      CameraControl.camera.shutter_speed = CameraControl.params['shutter_speed']
    CameraControl.camera.awb_gains = (CameraControl.params['wb_rg'] ,CameraControl.params['wb_gb'] )
    CameraControl.camera.drc_strength = CameraControl.params['drc_strength']
    CameraControl.camera.brightness = CameraControl.params['brightness']

  def shoot_image_max_resol(self):
    CameraControl.camera.resolution = self.params['max_resol']
    CameraControl.image_stream = BytesIO()
    CameraControl.camera.capture(CameraControl.image_stream, 'png')

  def shoot_image_using_jpeg(self):
    CameraControl.camera.resolution = self.params['max_resol']
    CameraControl.image_stream = BytesIO()
    temp_jpeg = '/tmp/jpeg_caputre.tmp'
    self.shutterSound()
    CameraControl.camera.capture(temp_jpeg, 'jpeg',quality=CameraControl.params['jpeg_quality'])
    #
    # Add Exif
    #   orientation
    #
    exif_dict = piexif.load(temp_jpeg)
    exif_dict["0th"][piexif.ImageIFD.Orientation] = CameraControl.params['Orientation']
    #piexif.remove(temp_jpeg)
    exif_bytes = piexif.dump(exif_dict)
    piexif.insert(exif_bytes,temp_jpeg)
    #
    # Read file into BytesIO stream
    #
    f = open(temp_jpeg,'rb')
    image_stream = BytesIO()
    CameraControl.image_stream.write(f.read())
    f.close()
    os.remove(temp_jpeg)

  def shoot_image_preview_resol(self):
    CameraControl.camera.resolution = CameraControl.params['preview_resol']
    CameraControl.preview_stream = BytesIO()
    CameraControl.camera.capture(CameraControl.preview_stream, 'bmp') # preview format is BMP !!

  #-- json
  def json_encode(self):
    js = json.dumps(CameraControl.params)
    return js
  def json_decode(self):
    d = json.loads(CameraControl.params)
    return d
  def json_load(self,file):
    f = open(file,'r')
    CameraControl.params = json.load(f)
  def json_load_from_string(self,str):
    CameraControl.params = json.loads(str)
  def json_save(self,file):
    f = open(file,'w')
    json.dump(CameraControl.params,f,indent=4,sort_keys=True)


#=================================
class MutiCastServer (CameraControl ,threading.Thread):

  MCAST_GRP = '239.2.1.1'
  MCAST_PORT = 27781
  SENDBACK_PORT = 27782

  def get_ip_address(self,ifname):
    s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    ip = socket.inet_ntoa(fcntl.ioctl(s.fileno(),0x8915, struct.pack('256s', ifname[:15].encode('utf-8')))[20:24])
    s.close()
    return ip

  def send_string_to_host(self, host_ip ,str):
      # s = socket.socket(socket.AF_INET, socket.SOCK_STREAM) # もとは TCP
      s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP) # UDPに変更
      s.connect((host_ip, self.SENDBACK_PORT))
      s.send(str.encode('UTF8'));
      s.close()

  def send_to_orientation(self,host_ip):
      send_data =  'ORI ' +  str(self.get_orientation())
      self.send_string_to_host(host_ip ,send_data)

  def run(self):
    logging.debug('start multicast thread')

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(('', self.MCAST_PORT))
    mreq = struct.pack("4sl", socket.inet_aton(self.MCAST_GRP), socket.INADDR_ANY)

    sock.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)
    id = self.get_ip_address('eth0')
    ip1, ip2, ip3, ip4 = id.split('.')


    while True:
      rcvdata = sock.recvfrom(1024)
      data = rcvdata[0].strip()
      cmd = data.decode('UTF-8')

      host_addr = rcvdata[1]

      logging.debug(cmd)
      if cmd == "RBT":
        cmd = 'sudo reboot'
        pid = subprocess.call(cmd, shell=True)
      elif cmd == "SDW":
        #cmd = 'amixer set PCM 90%'
        #pid = subprocess.run(cmd, shell=True)
        #cmd = 'aplay /home/pi/picamscan/sound/Shutdown.wav'
        pid = subprocess.run(cmd, shell=True)
        cmd = 'sudo poweroff'
        pid = subprocess.call(cmd, shell=True)
      elif cmd == "INQ":
        # response
        self.send_string_to_host(host_addr[0],'ACK');
      elif cmd == "ORI":
        # get orientation
        self.send_to_orientation(host_addr[0]);
      elif cmd == "SHT":
        # shoot
        self.lock()
        logging.debug('locked ... shoot start')
        self.shoot_image_max_resol()
        logging.debug('shoot end')
        self.unlock()
        logging.debug('unlocked ... shoot')
      elif cmd == "SHJ":
        # shoot jpeg
        self.lock()
        logging.debug('locked ... shoot start')
        logging.debug('shoot start')
        self.shoot_image_using_jpeg()
        logging.debug('shoot end')
        self.unlock()
        logging.debug('unlocked ... shoot')

      #---
      # Nerver reached
      #---


#=================================
class ShootImageServer (CameraControl ,threading.Thread):

  #-- socket commnication
  # v1.5
  def setPort(self,port):
    self.port = port

  def send_image(self,sock,stream):

    size =  stream.tell()
    size_bytes = size.to_bytes(4,'little')
    sock.send(size_bytes)

    stream.seek(0)
    image_buff = stream.read()
    sock.send(image_buff)
    sock.close()
    stream.close()

  def run(self):
    logging.debug('start tcp/ip comm thread')

    ADDR = ("",self.port)
    serv = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    serv.bind(ADDR)
    serv.listen(5)
    logging.debug('ready to commnication ... wait for client')

    while True:
      conn, addr = serv.accept()
      logging.debug('connect ')

      # get command
      data = conn.recv(4)
      cmd = data.decode('UTF-8')
      logging.debug(cmd)

      self.lock()

      if cmd == 'PRV':
        logging.debug('capture and get preview image')
        self.shoot_image_preview_resol()
        self.send_image(conn,self.preview_stream)
      elif cmd == "IMG":
        logging.debug('get full image')
        self.send_image(conn,self.image_stream);
      elif cmd == "O-N":
        logging.debug('set exif orientation 1 (north)')
        self.CameraControl.params['Orientation'] = 1
        self.json_save(json_init_file)
      elif cmd == "O-E":
        logging.debug('set exif orientation 8 (east)')
        self.CameraControl.params['Orientation'] = 1
        self.json_save(json_init_file)
      elif cmd == "O-S":
        logging.debug('set exif orientation 6 (south)')
        self.CameraControl.params['Orientation'] = 1
        self.json_save(json_init_file)
      elif cmd == "O-E":
        logging.debug('set exif orientation 3 (west)')
        self.CameraControl.params['Orientation'] = 1
        self.json_save(json_init_file)
      elif cmd == "PST":
        logging.debug('set parametr')
        # get parametr file and apply
        str = 'ACK'
        conn.send(str.encode('UTF8'));
        bsize = conn.recv(4)
        size = int.from_bytes(bsize,'little')
        logging.debug(size)

        bjson = conn.recv(size)
        json = bjson.decode('UTF8')

        logging.debug(json)

        self.json_load_from_string(json)
        self.param_apply()

        self.json_save(json_init_file)

      elif cmd == "PGT":
        logging.debug('get parametr')
        str = self.json_encode()
        conn.send(str.encode('UTF8'));
      else:
        logging.debug('not a commad')
        
      conn.close()
      self.unlock()

#------ Start Main

# Create instance

mcs = MutiCastServer()

mcs.json_load(json_init_file)
mcs.param_apply()

mcs.start()
sis_list = list()
for i in range(1):    # 32 -> 1 へ変更
  sis = ShootImageServer()
  sis.setPort(27783+i)
  sis.start()
  sis_list.append(sis)

#--- Start up play sound ----
# play sound "start up"
#cmd = 'amixer set PCM 90%'
#pid = subprocess.run(cmd, shell=True)
#
#cmd = 'aplay /home/pi/picamscan/sound/Startup.wav'
#pid = subprocess.run(cmd, shell=True)
#
#cmd = 'amixer set PCM 100%'
#pid = subprocess.run(cmd, shell=True)

mcs.join()
for sis in sis_list:
  sis.join()
