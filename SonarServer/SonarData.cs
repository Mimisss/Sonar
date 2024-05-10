namespace SonarServer
{
    /*
     * Sonar data received from Arduino HC-SR04
     * ULTRASONIC sensor. Raw data are 3 bytes
     * [255, ANGLE, DISTANCE] and are transformed to
     * instances of SonarData. The data separator
     * (255) is ignored.
     * Author:  Dimitris Pantazopoulos
     * Updated: 2024-10-05
     */
    public struct SonarData
    {
        public byte Angle { get; set; }

        public byte Distance { get; set; }
    }
}
