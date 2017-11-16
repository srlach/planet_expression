import csv
import os
import fnmatch
import numpy
import sys
import re
import subprocess

pat = '([a-zA-Z]+)(\d+)'
pat_out = '([a-zA-Z]+)(\d+)(_)(out)'

mapping = {'classical' : 1, 'weather' : 2, 'stop' : 3, 'joke' : 4}

def flat(filename,label):
    flatShit = []
    with open(filename+".csv", 'rU') as csvfile:
        reader = csv.reader(csvfile, delimiter='\t', quotechar='|')
        x = list(reader)
        multiplier = len(x)/10
        for column in range(len(x[1])):
            for item in range(10):
                flatShit.append(x[item*multiplier+2][column])
        flatShit.insert(0,label)
        a = numpy.asarray(flatShit)
    numpy.savetxt(filename+"_out.csv", [a], delimiter=",", fmt="%s")
    print ("%s --> %s" % (filename, filename+"_out.csv"))

def main():
    file_list = []
    for root,dirs,files in os.walk("."):
        file_list = files
        
    for f in file_list:
        matches = re.findall(pat,f)
        smatches = re.findall(pat_out,f)
        if len(matches) > 0 and len(smatches) == 0:
            for m in matches:
                fname = m[0]+m[1]
                lbl = mapping[m[0]]
                flat(fname,lbl)
    
    #copy *_out.csv training.csv"
    os.system('copy *_out.csv training.csv')
    os.system('Training.exe training.csv')

if __name__ == '__main__':
    main()
