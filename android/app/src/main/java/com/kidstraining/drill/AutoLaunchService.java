package com.kidstraining.drill;

import android.app.Notification;
import android.app.NotificationChannel;
import android.app.NotificationManager;
import android.app.PendingIntent;
import android.app.Service;
import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.content.SharedPreferences;
import android.content.pm.ServiceInfo;
import android.os.Build;
import android.os.IBinder;
import android.provider.Settings;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

/**
 * Keeps a lightweight foreground service alive so it can listen for
 * ACTION_USER_PRESENT (screen unlock). On the first unlock of each calendar day
 * it brings MainActivity to the front. If the app lacks the "display over other
 * apps" permission it posts a reminder notification instead.
 */
public class AutoLaunchService extends Service {
    private static final String CHANNEL_GUARDIAN = "kt_guardian";
    private static final String CHANNEL_REMINDER = "kt_reminder";
    private static final int ID_GUARDIAN = 1;
    private static final int ID_REMINDER = 2;
    private static final String PREF_LAST_DATE = "auto_launch_date";

    private BroadcastReceiver unlockReceiver;

    @Override
    public void onCreate() {
        super.onCreate();
        createChannels();
        Notification ongoing = new Notification.Builder(this, CHANNEL_GUARDIAN)
            .setSmallIcon(R.drawable.ic_launcher_foreground)
            .setContentTitle("キッズとっくん")
            .setContentText("まいにちの 自動起動が 有効です")
            .setOngoing(true)
            .build();
        if (Build.VERSION.SDK_INT >= 34) {
            startForeground(ID_GUARDIAN, ongoing,
                ServiceInfo.FOREGROUND_SERVICE_TYPE_SPECIAL_USE);
        } else {
            startForeground(ID_GUARDIAN, ongoing);
        }
        unlockReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                onUserPresent();
            }
        };
        IntentFilter filter = new IntentFilter(Intent.ACTION_USER_PRESENT);
        if (Build.VERSION.SDK_INT >= 33) {
            registerReceiver(unlockReceiver, filter, Context.RECEIVER_EXPORTED);
        } else {
            registerReceiver(unlockReceiver, filter);
        }
    }

    @Override
    public int onStartCommand(Intent intent, int flags, int startId) {
        return START_STICKY;
    }

    private void onUserPresent() {
        SharedPreferences prefs =
            getSharedPreferences(MainActivity.PREFS_NAME, MODE_PRIVATE);
        String today = new SimpleDateFormat("yyyy-MM-dd", Locale.US).format(new Date());
        if (today.equals(prefs.getString(PREF_LAST_DATE, ""))) {
            return;
        }
        prefs.edit().putString(PREF_LAST_DATE, today).apply();
        Intent open = new Intent(this, MainActivity.class)
            .addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_SINGLE_TOP);
        if (Build.VERSION.SDK_INT < 23 || Settings.canDrawOverlays(this)) {
            startActivity(open);
        } else {
            postReminder(open);
        }
    }

    private void postReminder(Intent open) {
        PendingIntent tap = PendingIntent.getActivity(this, 0, open,
            PendingIntent.FLAG_UPDATE_CURRENT | PendingIntent.FLAG_IMMUTABLE);
        Notification reminder = new Notification.Builder(this, CHANNEL_REMINDER)
            .setSmallIcon(R.drawable.ic_launcher_foreground)
            .setContentTitle("キッズとっくん")
            .setContentText("とっくんの じかんだよ")
            .setContentIntent(tap)
            .setAutoCancel(true)
            .build();
        getSystemService(NotificationManager.class).notify(ID_REMINDER, reminder);
    }

    private void createChannels() {
        NotificationManager manager = getSystemService(NotificationManager.class);
        manager.createNotificationChannel(new NotificationChannel(
            CHANNEL_GUARDIAN, "キッズとっくん自動起動",
            NotificationManager.IMPORTANCE_LOW));
        manager.createNotificationChannel(new NotificationChannel(
            CHANNEL_REMINDER, "とっくんのお知らせ",
            NotificationManager.IMPORTANCE_HIGH));
    }

    @Override
    public void onDestroy() {
        if (unlockReceiver != null) {
            unregisterReceiver(unlockReceiver);
        }
        super.onDestroy();
    }

    @Override
    public IBinder onBind(Intent intent) {
        return null;
    }
}
