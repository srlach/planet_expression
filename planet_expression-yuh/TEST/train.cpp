/* 
Properties
Edit------
* Output Directory: C:\Users\gamez\Downloads\grt\TEST
* Target name: 		train
* Call:				train.exe <train_file>
*/

//You might need to set the specific path of the GRT header relative to your project
#include <GRT/GRT.h>
using namespace GRT;
using namespace std;

int main(int argc, const char * argv[])
{
	//Parse the data filename from the argument list
	if (argc != 2) {
		cout << "Error: failed to parse data filename from command line. You should run this example with two argument pointing to the data filename!\n";
		return EXIT_FAILURE;
	}
	const string trainfile = argv[1];
	
	//Create a new AdaBoost instance
	AdaBoost adaBoost;

	//Set the weak classifier you want to use
	adaBoost.setWeakClassifier(DecisionStump());

	//Load some training data to train the classifier
	ClassificationData testData;
	
	cout << "Loading training data...\n";
	if (!trainingData.load(trainfile)) {
		cout << "Failed to load training data: " << trainfile << endl;
		return EXIT_FAILURE;
	}
	
	//Use 20% of the training dataset to create a test dataset
	ClassificationData testData = trainingData.split(80);
	
	cout << "Training data...\n";
	//Train the classifier
	if (!adaBoost.train(trainingData)) {
		cout << "Failed to train classifier!\n";
		return EXIT_FAILURE;
	}

	
	//Save the model to a file
	if (!adaBoost.save("AdaBoostModel.grt")) {
		cout << "Failed to save the classifier model!\n";
		return EXIT_FAILURE;
	}


	//Load the model from a file
	if (!adaBoost.load("AdaBoostModel.grt")) {
		cout << "Failed to load the classifier model!\n";
		return EXIT_FAILURE;
	}

	cout << "Testing data...\n";
	//Use the test dataset to test the AdaBoost model
	double accuracy = 0;
	for (UINT i = 0; i<testData.getNumSamples(); i++) {
		//Get the i'th test sample
		UINT classLabel = testData[i].getClassLabel();
		VectorFloat inputVector = testData[i].getSample();

		//Perform a prediction using the classifier
		if (!adaBoost.predict(inputVector)) {
			cout << "Failed to perform prediction for test sampel: " << i << "\n";
			return EXIT_FAILURE;
		}

		//Get the predicted class label
		UINT predictedClassLabel = adaBoost.getPredictedClassLabel();
		double maximumLikelhood = adaBoost.getMaximumLikelihood();
		VectorFloat classLikelihoods = adaBoost.getClassLikelihoods();
		VectorFloat classDistances = adaBoost.getClassDistances();

		//Update the accuracy
		if (classLabel == predictedClassLabel) accuracy++;

		cout << "TestSample: " << i << " ClassLabel: " << classLabel;
		cout << " PredictedClassLabel: " << predictedClassLabel << " Likelihood: " << maximumLikelhood;
		cout << endl;
	}

	cout << "Test Accuracy: " << accuracy / double(testData.getNumSamples())*100.0 << "%" << endl;

	return EXIT_SUCCESS;
}
