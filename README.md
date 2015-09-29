---
services: iot-hub
platforms: mbed, c, cpp
author: olivierbloch
---


# Simple temperature alert with Azure IoT Hub and an mbed board
Simple IoT project using Azure IoT Hub and an mbed device sending telemetry data. IoT Hub is used to ingest data from the device, and to notify the devie when alerts are triggered by Cloud services.
To learn more about Azure IoT Hub, check out the [Azure IoT Dev center].

## Running this sample
### Hardware prerequisites
In order to run this sample you will need the following hardware:
  - [mbed-enabled Freescale FRDM-K64F](https://developer.mbed.org/platforms/FRDM-K64F/)
  - [mbed Application shield](https://developer.mbed.org/components/mbed-Application-Shield/)
  - Both can be found together in the [mbed Ethernet IoT Started Kit](https://developer.mbed.org/platforms/IBMethernetKit/)

### Software prerequisites
  - These instructions assume that you have created an Azure IoT Hub instance. To learn how to do this, visit [this page](LINK TO AZURE IOT HUB SETUP PAGE ON AZURE.COM).
  - Visual Studio 2015
  - Serial terminal, such as [PuTTY](http://www.putty.org/), so you monitor debug traces from the devices.

### Services setup
In order to run the sample you will need to do the following:
  - Create an IoT hub that will receive data from devices and send commands back to it
  - Create an Event hub into which we will post alerts
  - Create a Stream Analytics job that will read data from the IoT hub and post alerts into the Event hub
  - Create a Storage account that will be used by the worker role
  - Deploy a simple worker role that will read alerts from the Event hub and forward alerts to devices through the IoT hub

#### Create an IoT Hub

1. Log on to the [Azure Preview Portal].

2. In the jumpbar, click **New**, then click **Internet of Things**, and then click **IoT Hub**.

![][iothub1]

3. In the **New IoT Hub** blade, specify the desired configuration for the IoT Hub.

![][iothub2]

    * In the **Name** box, enter a name to identify your IoT hub. When the **Name** is validated, a green check mark appears in the **Name** box.
    * Change the **Pricing and scale tier** as desired. This tutorial does not require a specific tier.
    * In the **Resource group** box, create a new resource group, or select and existing one. For more information, see [Using resource groups to manage your Azure resources](resource-group-portal.md).
    * Use **Location** to specify the geographic location in which to host your IoT hub.  


4. Once the new IoT hub options are configured, click **Create**.  It can take a few minutes for the IoT hub to be created.  To check the status, you can monitor the progress on the Startboard. Or, you can monitor your progress from the Notifications section.

![][iothub3]

5. After the IoT hub has been created successfully, open the blade of the new IoT hub, take note of the URI, and select the **Key** icon on the top.

![][iothub4]

6. Select the Shared access policy called **iothubowner**, then copy and take note of the connection string on the right blade.

![][iothub5]

Your IoT hub is now created, and you have the URI and connection string you need to complete this tutorial

For the creation of the Stream Analytics job Input, you will need to retreive some informations from the IoT Hub
  - From the Messaging blade (found in the settings blade), write down the **Event Hub-compatible name**
  - Look at the **Event-hub-compatifle Endpoint**, and write down this part: sb://**thispart**.servicebus.windows.net/ we will call this one the **IoTHub EventHub-compatible namespace**
  - From the "shared access policies" blade, select **iothubowner** and write down the **Primary Key**
    

#### Create an Event hub
1. Log on to the [Azure Preview Portal].

2. In the jumpbar, click **New**, then click **Internet of Things**, and then click **Event Hub**

3. Enter the following settings for the Event Hub:
  - Event Hub Name: "tempanomalyalerts"
  - Region: your choice
  - Subscription: your choice
  - Namespace Name: tempanomalyalerts-ns
  
 4. Click on **Create Event Hub**
 
 5. Select the tempanomalyalerts-ns and go in the **Event Hub** tab
 
 6. Select tthe tempanomalyalerts event hub and go in the **Configure** tab
 
 7. in the **Shared Access Policies** section, add a new policy:
  - Name = "readwrite"
  - Permissions = Send, Listen
  
 8. Click **Save**, then go to the evnet hub **Dashboard** tab and click on **Connection Information** at the bottom
 
 9. Write down the connection string for the readwrite policy name.


#### Create a Stream Analytics job
1. Log on to the [Azure Preview Portal].

2. In the jumpbar, click **New**, then click **Internet of Things**, and then click **Azure Stream Analytics**.

3. Enter a name for the job, a prefered region, choose your subscription.

4. Once the job is created, go to the **Inputs** tab and add an input. Select **Data stream**, then **Event Hub**

5. For the Event Hub settings, enter the following:
  - Input Alias = "tempsensors"
  - Subscription = "Use Event Hub from Another Subscription"
  - Service Bus Namespace = use the **IoTHub EventHub-compatible namespace** from the IoT Hub settings
  - Event Hub Nane = use the **Event Hub-compatible name** from the IoT Hub settings
  - Event Hub Policy Name = "iothubowner"
  - Event Hub Policy Key = 
  - Event Hub Consumer Group = ""

6. Select JSON - UTF8 for the serialization settings

7. In the **Query** tab, type in the below query and click **Save** below

  ```
  SELECT
      System.timestamp AS timestart,
      ObjectName AS dsplalert,
      ObjectType AS alerttype,
      Version AS message,
      TargetAlarmDevice AS targetalarmdevice
  INTO
      eventhub
  FROM
      tempsensors
  WHERE temp>82
  ```

8. In the **Outputs** tab, select **Add an Output**

9. Choose **Event Hub** and enter the following settings:
  - Output Alias = "eventhub"
  - Subscription = Pick the one you created the event hub in during previous step
  - Choose a Namespace = "tempanomalyalerts-ns"
  - Choose and EventHub = "tempanomalyalerts"
  - Event Hub Policy Name = "readwrite"
  
10. choose JSON, UTF8, Line separated for the serialization settings

11. Start the job by clicking on **Start** at the bottom

#### Create a storage account
1. Log on to the [Azure Preview Portal].

2. in the jumpbar, click **New** and select **Data + Storage** then **Storage Account**

3. Choose **Classic** for the deployment model and click on **create**

4. Enter the name "tempanomalystorage" for the account name and select your resource group, subscription,... then click on "Create"

5. Once the account is created, find it in the resources blade and write down the primary connection string for it to configure the worker role


#### Deploy the worker role
The sample uses a worker role to trigger alerts back on devices through IoT Hub.
To build an deploy the worker role here are the few simple steps:

1. Clone the repository on your machine

2. Open the solution events_to_device_service\events_to_device_service.sln in Visual Studio 2015

3. open the file app.config and replace the fields below with the connection strings from the event hub, the storage account and the Iot Hub

    ```
    <add key="Microsoft.ServiceBus.ConnectionString" value="[EventHub Connection String]" />
    <add key="Microsoft.ServiceBus.EventHubName" value="[Event Hub Name]" />
    <add key="AzureStorage.AccountName" value="[Storage Account Name]" />
    <add key="AzureStorage.Key" value="[Storage Account Key]" />
    <add key="AzureIoTHub.ConnectionString" value="[IoT Hub Connection String]" />
    ```
4. Compile the project and publish to Azure


#### Create a new device identity in the IoT Hub
To connect your device to the IoT Hub instance, you need to generate a unique identity and connection string. IoT Hub does that for you.
To create a new device identity, you have the following options:
- Use the [Device Explorer tool][device-explorer] (runs only on Windows for now)
- Or use the node.js tool
  - For this one, you need to have node installed on your machine (https://nodejs.org/en/)
  - Once node is installed, in a command shell, type the following commands:

    ```
    npm install -g iothub-explorer
    ```

  - You can type the following to learn more about the tool
  
    ```
    iothub-explorer help
    ```
  
  - Type the following commands to create a new device identity (replace <connectionstring> in the commands with the connection string for the **iothubowner** that you retreive previously in the portal.
  
    ```
    iothub-explorer <connectionstring> create mydevice --connection-string
    ```

  - This will create a new device identity in your IoT Hub and will display the required information. Copy the connectionString
     
### Connect the device

- Connect the board to your network using an Ethernet cable. This step is required, as the sample depends on internet access.

- Plug the device into your computer using a micro-USB cable. Be sure to attach the cable to the correct USB port on the device, as pictured [here](https://developer.mbed.org/platforms/IBMEthernetKit/), in the "Getting started" section.

- Follow the [instructions on the mbed handbook](https://developer.mbed.org/handbook/SerialPC) to setup the serial connection with your device from your development machine. If you are on Windows, install the Windows serial port drivers located [here](http://developer.mbed.org/handbook/Windows-serial-configuration#1-download-the-mbed-windows-serial-port).

## Create mbed project and import the sample code

- In your web browser, go to the mbed.org [developer site](https://developer.mbed.org/). If you haven't signed up, you will see an option to create a new account (it's free). Otherwise, log in with your account credentials. Then click on **Compiler** in the upper right-hand corner of the page. This should bring you to the Workspace Management interface.

- Make sure the hardware platform you're using appears in the upper right-hand corner of the window, or click the icon in the right-hand corner to select your hardware platform.

- Click on **New** and in the pop up window,
  - Select the FRDM-K64F board (you might need to add it to your environement the very first time clicking on the logo in the top right corner)
  - pick the "Empty Program" template
  - name the program
  
![][mbed8]

- Select the newly created program in the Program WOrkspace and click **Import** on the main menu. Then click the **Click here** to import from URL link next to the mbed globe logo.

![][mbed1]

- In the popup window, enter the link for the embed library http://mbed.org/users/mbed_official/code/mbed/

![][mbed9]

- Repeat the import from URL operation for all the following libraries:
  - http://mbed.org/users/mbed_official/code/mbed/
  - https://developer.mbed.org/users/mbed_official/code/mbed-rtos/
  - https://developer.mbed.org/users/mbed_official/code/EthernetInterface/
  - https://developer.mbed.org/users/donatien/code/NTPClient/
  - https://developer.mbed.org/users/AzureIoTClient/code/iothub_client/
  - https://developer.mbed.org/users/AzureIoTClient/code/iothub_amqp_transport/
  - https://developer.mbed.org/users/AzureIoTClient/code/proton-c-mbed/
  - https://developer.mbed.org/users/wolfSSL/code/wolfSSL/
  - https://developer.mbed.org/users/AzureIoTClient/code/serializer/
  - https://developer.mbed.org/users/chris/code/C12832/
  - https://developer.mbed.org/users/neilt6/code/LM75B/
  
- Right click on the program folder, select **New File** and create a new file named main.cpp

- Copy the content from the [main.cpp repository file](./mbed/main.cpp) and paste it in the new main.cpp

- In the temp_sensor_anomaly\main.c file, replace the highlighted code with your device connection string (to obtain this device connection string you can use the node.js tool as described earlier in this tutorial or using device explorer as instructed [here][device-explorer]):

	![][mbed4]

- Click **Compile** to build the program. You can safely ignore any warnings, but if the build generates errors, fix them before proceeding.

- If the build is successful, a .bin file with the name of your project is generated. Copy the .bin file to the device. Saving the .bin file to the device causes the current terminal session to the device to reset. When it reconnects, reset the terminal again manually, or start a new terminal. This enables the mbed device to reset and start executing the program.

- Connect to the device using an serial terminal client application, such as PuTTY. You can determine which serial port your device uses by checking the Windows Device Manager:

	![][mbed6]
  
- In PuTTY, click the **Serial** connection type. The device most likely connects at 115200, so enter that value in the **Speed** box. Then click **Open**: 

	![][mbed7]

The program starts executing. You may have to reset the board (press CTRL+Break or press on the board's reset button) if the program does not start automatically when you connect.

Now your device is sending telemetry data to Azure through IoT Hub.
The Stream Analytics job is doing near real time analytics on the device data and will post alerts based on the SQL request it is configured with.
The Worker role will pick up the alerts generated by Stream Analytics and will forward the alerts to the device through IoT Hub.
The device will start bleeping and will display the name of the device that triggered the alert.
You can push data from several devices, provisionning them with several connection strings that you can generate as instructed above. 

## More information
To learn more about Azure IoT Hub check out the [Azure IoT Dev center].
In the IoT dev center  you will also find plenty simple samples for connecting many sorts of devices to Azure IoT Hub.
You can replace the mbed board in this sample with any of the supported devices by simply adapting the meta data and the format of the data sent to the services set up in this sample.


[Azure Preview Portal]: https://portal.azure.com/
[Azure IoT Dev center]: https://www.azure.com/iotdev
[device-explorer]: http://aka.ms/iot-hub-how-to-use-device-explorer
[iothub1]: ./media/create-iot-hub1.png
[iothub2]: ./media/create-iot-hub2.png
[iothub3]: ./media/create-iot-hub3.png
[iothub4]: ./media/create-iot-hub4.png
[iothub5]: ./media/create-iot-hub5.png

[mbed1]: ./media/mbed1.png
[mbed2]: ./media/mbed2.png
[mbed3]: ./media/mbed3.png
[mbed4]: ./media/mbed4.png
[mbed6]: ./media/mbed6.png
[mbed7]: ./media/mbed7.png
[mbed8]: ./media/mbed8.png
[mbed9]: ./media/mbed9.png
[mbed10]: ./media/mbed10.png