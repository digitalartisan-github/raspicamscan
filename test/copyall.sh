#!/bin/bash
for ((i=12; i<=45; ++i))
do
        echo $i
        sshpass -p 'kyozou' scp -o StrictHostKeyChecking=no syncshooter.py pi@192.168.55.$i:/home/pi/picamscan/