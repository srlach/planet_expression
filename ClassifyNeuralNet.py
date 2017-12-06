import numpy as np
import csv 
from sklearn import metrics
from sklearn.externals import joblib
import time
import pickle
import sys


def classify(file):
    d = open(file)
 
    reader = csv.reader(d)
    data = []
    for row in reader:
        temp = []
        for item in row:
            temp.append(float(item.strip()))
        data.append(temp)

    data = np.array(data)
    docs = data[:,1:]

    clf = joblib.load('NeuralNetworks.pkl')
    
    doc_test = docs

    predicted = clf.predict(doc_test)
   
    for i in range(len(data)):
        print "TestSample: %s ClassLabel: %s PredictedClassLabel: %s" % (i,int(data[i][0]),int(predicted[i]))

def main(file):
    classify(file)


if __name__ == "__main__":
    if (len(sys.argv) != 2):
        print ('Incorrect number of arguments!')
    main(sys.argv[1])
