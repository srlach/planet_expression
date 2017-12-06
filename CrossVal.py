import numpy as np
import csv 
from sklearn.ensemble import AdaBoostClassifier
from sklearn.linear_model import SGDClassifier
from sklearn.neighbors import KNeighborsClassifier
from sklearn import svm
from sklearn.linear_model import LogisticRegression
from sklearn.tree import DecisionTreeClassifier
from sklearn.neural_network import MLPClassifier
from sklearn.ensemble import RandomForestClassifier
from sklearn import metrics
from sklearn.externals import joblib
from sklearn.model_selection import KFold
import time
import pickle
import sys

ada_clf = AdaBoostClassifier()
sgd_clf = SGDClassifier(max_iter=5,tol=None)
svm_clf = svm.SVC()
lr_clf = LogisticRegression()
knn_clf = KNeighborsClassifier()
dt_clf = DecisionTreeClassifier()
nn_clf = MLPClassifier()

classifiers = [ ada_clf, sgd_clf, svm_clf, lr_clf, knn_clf, dt_clf, nn_clf]


def classify(classifier, training, testing, trainingtarget, testingtarget):
 
    clf = classifier.fit(training, trainingtarget)
    predict = clf.predict(testing)

    predicted = clf.predict(testing)
   
    print "ACCURACY: ", np.mean(predicted == testingtarget)




def crossval(classifier,data):
    labels = data[:,0]
    docs = data[:,1:]

    kf = KFold(n_splits=10)
    count = 0
    print "======================================="
    print "CLASSIFIER: ", classifier
    for train, test in kf.split(docs):
        print "FOLD ", count
        traininggset = []
        testingset=[]
        traininglabel=[]
        testinglabel=[]
        for i in train:
            traininggset.append(docs[i])
            traininglabel.append(labels[i])
        for i in test:
            testingset.append(docs[i])
            testinglabel.append(labels[i])

        classify(classifier,traininggset,testingset, traininglabel,testinglabel)
        count+=1
        

    

def getData(file):
    d = open(file)
 
    reader = csv.reader(d)
    data = []
    for row in reader:
        temp = []
        for item in row:
            temp.append(float(item.strip()))
        data.append(temp)

    data = np.array(data)
    return data

def main(file):
    data = getData(file)
    for classifier in classifiers:
        crossval(classifier,data)


if __name__ == "__main__":
    if (len(sys.argv) != 2):
        print ('Incorrect number of arguments!')
    main(sys.argv[1])
