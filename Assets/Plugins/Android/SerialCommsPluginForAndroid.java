package com.mohajeranilab.serialcomms;

import android.Manifest;
import android.app.Activity;
import android.app.PendingIntent;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.pm.PackageManager;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbManager;
import android.os.Build;
import android.os.Bundle;
import android.widget.Toast;

import androidx.annotation.Nullable;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.PrivateKey;
import java.util.List;

import com.mohajeranilab.serialcomms.driver.UsbSerialDriver;
import com.mohajeranilab.serialcomms.driver.UsbSerialPort;
import com.mohajeranilab.serialcomms.driver.UsbSerialProber;

public class Plugin extends Activity {
    private static Plugin instance;

    private Context context;
    private UsbSerialPort port;
    private UsbDeviceConnection connection;

    public Plugin() {
        instance = this;
    }

    public static Plugin instance() {
        if (instance == null) {
            instance = new Plugin();
        }
        return instance;
    }

    public void setContext(Context context) {
        this.context = context;
    }

    public void showToastMessage(String msg) {
        Toast.makeText(this.context, msg, Toast.LENGTH_SHORT).show();
    }

    public int setupArduino() {
        UsbManager manager = (UsbManager) this.context.getSystemService(Context.USB_SERVICE);

        List<UsbSerialDriver> availableDrivers = UsbSerialProber.getDefaultProber().findAllDrivers(manager);
        if (availableDrivers.isEmpty()) {
            Toast.makeText(this.context, "No arduino device found", Toast.LENGTH_SHORT).show();
            return -1;
        }

        UsbSerialDriver driver = availableDrivers.get(0);
        connection = manager.openDevice(driver.getDevice());

        if (connection == null) {
            Intent intent = new Intent();
            intent.setClass(this.context, Context.USB_SERVICE.getClass());
            PendingIntent mPendingIntent = PendingIntent.getActivity(this.context, 0, intent, 0);
            manager.requestPermission(driver.getDevice(), mPendingIntent);
            Toast.makeText(this.context, "Permission requested, try again.", Toast.LENGTH_SHORT).show();
            return -1;
        }

        if (manager.hasPermission(driver.getDevice())) {
            port = driver.getPorts().get(0);
            try {
                port.open(connection);
                port.setParameters(9600, 8, UsbSerialPort.STOPBITS_1, UsbSerialPort.PARITY_NONE);
            } catch (IOException e) {
                Toast.makeText(this.context, "Problem occurred in connecting to arduino", Toast.LENGTH_SHORT).show();
            }
            Toast.makeText(this.context, "Successfully connected to arduino", Toast.LENGTH_SHORT).show();
        }
        return 0;
    }


    public int sendMessageToArduino(String message) {
        try {
            port.write((message + "\n").getBytes(StandardCharsets.UTF_8), 0);
        } catch (IOException e) {
            closeArduinoConnection();
            setupArduino();
            return -1;
        }
        return 0;
    }

    public int closeArduinoConnection() {
        if (port.isOpen()) {
            try {
                connection.close();
                port.close();
                return 0;
            } catch (IOException e) {
                return -1;
            }
        }
        return -1;
    }
}
