# GestureControl
Hand gesture recognition program developed on C# + WPF + EmguCV (latest).

## Project purpose
This project was created as EmguCV library usage training and as a small guide because there is no all working examples of using latest library version features in network exists.
Many different code upgrades were made since library version 4.x and not all of them are described in official documentation. So I decided to create this project for starting in computer vision sphere and helping others who needs examples of library usage.

## Project description
The project is at an early stage of development. It can find and mark contour of hand on the incoming video stream from the device webcam. All process contains 4 steps: preprocessing, morphological processing, edge detection and contour finding.
But now the program depends heavily on the level of illumination of the room and the reflectivity of the background. So it is needed to improve hand detection methods.
Operations on all steps are used from EmguCV library functionality. All theese steps are working in real-time mode on each frame of videoinput.

## Preprocessing
On this step input frame is converting to grayscale. After this it is processed to black and white image using manually set binarization treshold. Also it can be blurred using manually set blur treshold. In some cases a little blurring of frame may improve the results of further edge detection.

## Morphological processing
On this step the frame is processed by manually chosen morphological filtration method, filtration kernel type and size during the manually set number of processing iterations. Also there is an option of two-step filtration for combining different morphological filtration methods.
This step helps to minimize noises on the frame and improves further edge detection.

## Edge detection
On this step program is finding the contours of objects on processed frame using EmguCV built-in Canny edge detection method. Lower and upper tresholds for this algorithm can be manually set in the program interface. With the increasing of tresholds level just stronger contours will be founded.
All founded contours are saved in special EmguCV structure.

## Contours finding
On this step program is taking founded contours and is calculating their areas by built-in methods. After the largest contour is marked with red line on output frame. Also program is finding extreme points of contour called defects and mark them with blue filled circles. Theese points match fingertips of the hand.

## Program screenshot
![alt text](https://github.com/OverchenkoDev/GestureControl/blob/master/main_screen.jpg)

## Working results
Founded contours with points of defects will be used for matching different gesture templates in further development. It means that positions of defect points will be fixed on each frame and then after changing their moving will be matched with one of the hand gesture templates associated with some activity.

## Task list
- [x] Update EmguCV library to version 4.1.1
- [ ] Program perfomance improving
- [ ] Add some other edge detection algorithms
- [ ] Add adaptive skin color detector for improved hand detection
- [ ] Add gesture templates
- [ ] Add defects points moving processing
