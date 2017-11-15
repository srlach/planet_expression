import csv
import numpy

flatShit = []

with open('stuff.csv', 'rU') as csvfile:
    reader = csv.reader(csvfile, delimiter='\t', quotechar='|')
    x = list(reader)
    multiplier = len(x)/10
    for column in range(len(x[1])):
        for item in range(10):
            flatShit.append(x[item*multiplier][column])

print flatShit
