import math

buffer = [0]*20
avga=0
avgb=0
state = False
motionStatePrev = False
motionStateCurr = True
recStatePrev = False
recStateCurr = False
recSwitch = False

def push(x):
    global avga
    global avgb
    temp = x
    next = 0
    for i in range(len(buffer)):
        next = temp
        temp = buffer[i]
        buffer[i] = next
    
    a = buffer[:10]
    b = buffer[10:]
    avga = sum(a)/len(a)
    avgb = sum(b)/len(b)
    changeCheck()


    #print (buffer,avga,avgb,isRecording())
def changeCheck():
    motionCheck()
    isRecording()
    startMotion()
    stopMotion()

def motionCheck():
    global state
    global recStatePrev
    global recStateCurr
    recStatePrev = recStateCurr
    print (avga,avgb,abs(avga - avgb))
    if abs(avga - avgb) > avgb*.50 + 5:
        state = True
        recStateCurr = True
    else:
        state = False
        recStateCurr = False

def isRecording():
    global recSwitch
    if recSwitch == True:
        print "RECORDING"

def startMotion():
    global recStatePrev
    global recStateCurr
    global recSwitch
    if (recStateCurr == True and recStatePrev == False):
        print "----------START MOTION---------------"
        recSwitch = True

def stopMotion():
    global recStatePrev
    global recStateCurr
    global recSwitch
    if (recStateCurr == False and recStatePrev == True):
        print "----------STOP MOTION---------------"
        recSwitch = False
    
         

def main():
    for i in range(10): # no gesture
        push(0)

    for i in range(10):
        push(i)

    for i in range(100):
        push(100)

    for i in range(10):
        push(0)

    for i in range(10):
        push(0)





if __name__ == "__main__":
    main()

