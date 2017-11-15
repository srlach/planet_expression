import csv

import numpy

import sys



flatShit = []



with open(sys.argv[1]+".csv", 'rU') as csvfile:

    reader = csv.reader(csvfile, delimiter='\t', quotechar='|')

    x = list(reader)

    multiplier = len(x)/10

    for column in range(len(x[1])):

        for item in range(10):

            flatShit.append(x[item*multiplier+2][column])

    flatShit.insert(0,sys.argv[2])

    a = numpy.asarray(flatShit)



numpy.savetxt(sys.argv[1]+"_out.csv", [a], delimiter=",", fmt="%s")
