package com.kidstraining.drill;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Build;

/**
 * Restarts the unlock watcher after a device reboot, but only while the parent
 * menu's daily auto-launch toggle is enabled.
 */
public class BootReceiver extends BroadcastReceiver {
    @Override
    public void onReceive(Context context, Intent intent) {
        if (!Intent.ACTION_BOOT_COMPLETED.equals(intent.getAction())) {
            return;
        }
        SharedPreferences prefs =
            context.getSharedPreferences(MainActivity.PREFS_NAME, Context.MODE_PRIVATE);
        if (!prefs.getBoolean(MainActivity.PREF_AUTO_LAUNCH, false)) {
            return;
        }
        Intent service = new Intent(context, AutoLaunchService.class);
        if (Build.VERSION.SDK_INT >= 26) {
            context.startForegroundService(service);
        } else {
            context.startService(service);
        }
    }
}
