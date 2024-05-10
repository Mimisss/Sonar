/*
 * Arduino Sonar using Arduino Uno R3, Servo and HC-SR04 ULTRASONIC 
 * sensor. The NewPing library is used to get measurements from the 
 * HC-SR04. Sonar data are sent to the serial monitor in the following
 * format: [255, ANGLE, DISTANCE] where ANGLE is a value between
 * 0 and 180, DISTANCE is a value between 0 and 100 measured in 
 * cm and 255 is a separator value to distinguish ANGLE,DISTANCE
 * pairs. The device is ready to go when the green LED is turned on.
 * 
 * Based on:  https://en.morzel.net/post/OoB-Sonar-with-Arduino-C-JavaScript-and-HTML5
 *            by MiLosz Orzel
 * Author:    Dimitris Pantazopoulos
 * Version:   sonar-serial-r3
 * Updated:   2024-05-10
 */

#include <Servo.h>
#include <NewPing.h>

const byte ledPin = 8;
const byte triggerPin = 10;
const byte echoPin = 11;
const byte servoPin = 12;

const byte maxDistanceInCm = 100;

const byte dataSeparatorValue = 255;

byte angle;
byte angleStep;
byte delayInMs = 50;

NewPing sonar(triggerPin, echoPin, maxDistanceInCm);
Servo servo;

void setup() {
  // put your setup code here, to run once:
  pinMode(ledPin, OUTPUT);
  digitalWrite(ledPin, LOW);

  angle = 0;
  angleStep = 1;
  
  // init servo
  servo.attach(servoPin);
  servo.write(angle);

  // init serial
  Serial.begin(9600);
  
  // ready to go
  digitalWrite(ledPin, HIGH);
}

void loop() {
  // put your main code here, to run repeatedly:
  toggleServoDirection();

  getDistance();

  // move servo
  angle += angleStep;
  servo.write(angle);

  delay(delayInMs);
}

void toggleServoDirection() {
  // Servo is moving from 0 to 180 and 
  // from 180 down to 0 continuously. angleStep
  // determines the moving direction.
  if (angle == 180) {
    angleStep = -1;
  } else if (angle == 0) {
    angleStep = 1;
  }
}

void getDistance() {    
  // use ultrasound to measure distance
  byte distanceInCm = sonar.ping_cm();

  // write array of bytes to serial port. Each time the array
  // contains a pair of angle-distance values and a separator
  // value (255) to distinguish between them.
  byte sonarData[] = {dataSeparatorValue, angle, distanceInCm};
  Serial.write(sonarData, 3);        
}