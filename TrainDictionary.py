import numpy as np
import csv 
from sklearn.feature_extraction.text import CountVectorizer
from sklearn.feature_extraction.text import TfidfTransformer
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
import sys
import time
import pickle


def train(data):
    d = open(data)
    t = open(data)
    reader = csv.reader(d)
    data = []
    for row in reader:
        temp = []
        for item in row:
            #print item
            temp.append(float(item.strip()))
        data.append(temp)

    data = np.array(data)

    label = data[:,0]

    ada_clf = AdaBoostClassifier().fit(data,label)
    sgd_clf = SGDClassifier(max_iter=5,tol=None).fit(data, label)
    svm_clf = svm.SVC().fit(data, label)
    lr_clf = LogisticRegression().fit(data, label)
    knn_clf = KNeighborsClassifier().fit(data, label)
    dt_clf = DecisionTreeClassifier().fit(data, label)
    nn_clf = MLPClassifier().fit(data, label)

    joblib.dump(ada_clf, 'ADA.pkl')
    joblib.dump(sgd_clf, 'SGD.pkl')
    joblib.dump(svm_clf, 'SVM.pkl')
    joblib.dump(lr_clf, 'LogisticRegression.pkl')
    joblib.dump(knn_clf, 'KNN.pkl')
    joblib.dump(dt_clf, 'DecisionTree.pkl')
    joblib.dump(nn_clf, 'NeuralNetworks.pkl')
    
    doc_test = data
    target_test = label

    predicted1 = ada_clf.predict(doc_test)
    predicted2 = sgd_clf.predict(doc_test)
    predicted3 = svm_clf.predict(doc_test)
    predicted4 = lr_clf.predict(doc_test)
    predicted5 = knn_clf.predict(doc_test)
    predicted6 = dt_clf.predict(doc_test)
    predicted7 = nn_clf.predict(doc_test)

    print "accuracy AdaBoost: ", np.mean(predicted1 == target_test)
    print "accuracy Stochiastic Gradient Descent: ", np.mean(predicted2 == target_test)
    print "accuracy SVM: ", np.mean(predicted3 == target_test)
    print "accuracy Logistic Regression: ", np.mean(predicted4 == target_test)
    print "accuracy Nearest Neighbor: ", np.mean(predicted5 == target_test)
    print "accuracy Decision Tree: ", np.mean(predicted6 == target_test)
    print "accuracy Neural Networks: ", np.mean(predicted7 == target_test)

    for i in range(len(data)):
        print "TestSample: %s ClassLabel: %s PredictedClassLabel: %s" % (i,int(data[i][0]),int(predicted1[i]))

def main(file):
    print("Training...")
    train(file)


if __name__ == "__main__":
    if (len(sys.argv) != 2):
        print ('Incorrect number of arguments!')
    main(sys.argv[1])