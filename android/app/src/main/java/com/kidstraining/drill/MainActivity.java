package com.kidstraining.drill;

import android.annotation.SuppressLint;
import android.app.Activity;
import android.content.Intent;
import android.content.SharedPreferences;
import android.net.Uri;
import android.os.Build;
import android.os.Bundle;
import android.provider.Settings;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;
import android.view.View;
import android.webkit.JavascriptInterface;
import android.webkit.WebChromeClient;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;

import java.util.Locale;

public class MainActivity extends Activity {
    static final String PREFS_NAME = "kt_settings";
    static final String PREF_AUTO_LAUNCH = "auto_launch";

    private static final String LEARNING_PAGE_URL = "file:///android_asset/kids-training.runtime.html";

    // The drill flow is a single-page app: Android back should step out of the
    // current drill screen the same way the on-screen やめる/もどる buttons do.
    private static final String BACK_STEP_SCRIPT =
        "(function(){var b=document.querySelector('.kt-drill-quit');if(b){b.click();return true;}"
        + "b=document.querySelector('.kt-drill-mode-back');if(b){b.click();return true;}"
        + "b=document.querySelector('.kt-drill-back');if(b){b.click();return true;}"
        + "b=document.querySelector('.kt-mobile-back');if(b){b.click();return true;}"
        + "return false;})()";

    private WebView webView;
    private SharedPreferences prefs;
    // Japanese text-to-speech for the drill's correct answers (issue #105). The bridge reads it
    // from the JavaScript thread, so both fields are volatile; ttsReady stays false until the
    // engine has a Japanese voice, and the page then simply shows the answer without reading it.
    private volatile TextToSpeech tts;
    private volatile boolean ttsReady;

    @SuppressLint({"SetJavaScriptEnabled", "AddJavascriptInterface"})
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        applyImmersiveMode();
        prefs = getSharedPreferences(PREFS_NAME, MODE_PRIVATE);
        webView = new WebView(this);
        WebSettings settings = webView.getSettings();
        settings.setJavaScriptEnabled(true);
        settings.setDomStorageEnabled(true);
        settings.setMediaPlaybackRequiresUserGesture(false);
        webView.setWebViewClient(new WebViewClient());
        webView.setWebChromeClient(new WebChromeClient());
        webView.addJavascriptInterface(new AndroidBridge(), "KtAndroid");
        setContentView(webView);
        startSpeech();

        if (prefs.getBoolean(PREF_AUTO_LAUNCH, false)) {
            startAutoLaunchService();
        }
        if (savedInstanceState != null) {
            webView.restoreState(savedInstanceState);
        } else {
            webView.loadUrl(LEARNING_PAGE_URL);
        }
    }

    private void startSpeech() {
        tts = new TextToSpeech(this, status -> {
            TextToSpeech engine = tts;
            if (status != TextToSpeech.SUCCESS || engine == null) {
                return;
            }
            engine.setOnUtteranceProgressListener(new UtteranceProgressListener() {
                @Override
                public void onStart(String utteranceId) {
                }

                @Override
                public void onDone(String utteranceId) {
                    notifySpeechEnd(utteranceId);
                }

                @Override
                public void onError(String utteranceId) {
                    notifySpeechEnd(utteranceId);
                }
            });
            int result = engine.setLanguage(Locale.JAPAN);
            ttsReady = result != TextToSpeech.LANG_MISSING_DATA && result != TextToSpeech.LANG_NOT_SUPPORTED;
        });
    }

    /** Tells the page that the reading it started with KtAndroid.speak has finished. */
    private void notifySpeechEnd(String utteranceId) {
        String id = digitsOnly(utteranceId);
        if (id.isEmpty()) {
            return;
        }
        runOnUiThread(() -> {
            if (webView != null) {
                webView.evaluateJavascript(
                    "window.__ktDrillSpeechEnd&&window.__ktDrillSpeechEnd('" + id + "')", null);
            }
        });
    }

    // Utterance ids come from the page and go back into a script, so only digits survive.
    private static String digitsOnly(String value) {
        return value == null ? "" : value.replaceAll("[^0-9]", "");
    }

    private void startAutoLaunchService() {
        Intent service = new Intent(this, AutoLaunchService.class);
        if (Build.VERSION.SDK_INT >= 26) {
            startForegroundService(service);
        } else {
            startService(service);
        }
    }

    /**
     * Bridge exposed to the generated runtime as window.KtAndroid. The parent
     * dashboard's "Android せってい" card uses it for the daily auto-launch toggle
     * and the Android settings shortcuts, and the drill uses speak/stopSpeaking to
     * read correct answers aloud. Calls arrive on a background thread,
     * so anything touching the activity hops back to the UI thread.
     */
    private class AndroidBridge {
        @JavascriptInterface
        public boolean getAutoLaunch() {
            return prefs.getBoolean(PREF_AUTO_LAUNCH, false);
        }

        @JavascriptInterface
        public void setAutoLaunch(boolean enabled) {
            prefs.edit().putBoolean(PREF_AUTO_LAUNCH, enabled).apply();
            runOnUiThread(() -> {
                if (enabled) {
                    startAutoLaunchService();
                } else {
                    stopService(new Intent(MainActivity.this, AutoLaunchService.class));
                }
            });
        }

        @JavascriptInterface
        public boolean canDrawOverlays() {
            return Build.VERSION.SDK_INT < 23
                || Settings.canDrawOverlays(MainActivity.this);
        }

        @JavascriptInterface
        public void openOverlaySettings() {
            runOnUiThread(() -> {
                if (Build.VERSION.SDK_INT >= 23) {
                    startActivity(new Intent(
                        Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
                        Uri.parse("package:" + getPackageName())));
                }
            });
        }

        @JavascriptInterface
        public void openNotificationSettings() {
            runOnUiThread(() -> {
                if (Build.VERSION.SDK_INT >= 33) {
                    requestPermissions(
                        new String[]{android.Manifest.permission.POST_NOTIFICATIONS}, 1001);
                } else {
                    startActivity(new Intent(Settings.ACTION_APP_NOTIFICATION_SETTINGS)
                        .putExtra(Settings.EXTRA_APP_PACKAGE, getPackageName()));
                }
            });
        }

        @JavascriptInterface
        public void openBatterySettings() {
            runOnUiThread(() -> {
                try {
                    startActivity(new Intent(
                        Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS,
                        Uri.parse("package:" + getPackageName())));
                } catch (Exception e) {
                    startActivity(new Intent(
                        Settings.ACTION_IGNORE_BATTERY_OPTIMIZATION_SETTINGS));
                }
            });
        }

        /**
         * Reads text aloud in Japanese and returns whether it started. When it ends, the
         * page's window.__ktDrillSpeechEnd(id) runs with the same digits-only id.
         */
        @JavascriptInterface
        public boolean speak(String text, String id) {
            TextToSpeech engine = tts;
            String utteranceId = digitsOnly(id);
            if (!ttsReady || engine == null || text == null || text.isEmpty() || utteranceId.isEmpty()) {
                return false;
            }
            return engine.speak(text, TextToSpeech.QUEUE_FLUSH, null, utteranceId) == TextToSpeech.SUCCESS;
        }

        @JavascriptInterface
        public void stopSpeaking() {
            TextToSpeech engine = tts;
            if (engine != null) {
                engine.stop();
            }
        }
    }

    // ---- lifecycle / navigation ----

    @Override
    public void onWindowFocusChanged(boolean hasFocus) {
        super.onWindowFocusChanged(hasFocus);
        if (hasFocus) {
            applyImmersiveMode();
        }
    }

    @Override
    protected void onResume() {
        super.onResume();
        webView.onResume();
    }

    @Override
    protected void onPause() {
        webView.onPause();
        super.onPause();
    }

    @Override
    protected void onSaveInstanceState(Bundle outState) {
        super.onSaveInstanceState(outState);
        webView.saveState(outState);
    }

    @Override
    protected void onDestroy() {
        TextToSpeech engine = tts;
        tts = null;
        ttsReady = false;
        if (engine != null) {
            engine.stop();
            engine.shutdown();
        }
        webView.destroy();
        super.onDestroy();
    }

    @Override
    public void onBackPressed() {
        if (webView == null) {
            super.onBackPressed();
            return;
        }
        webView.evaluateJavascript(BACK_STEP_SCRIPT, handled -> {
            if (!"true".equals(handled)) {
                MainActivity.super.onBackPressed();
            }
        });
    }

    private void applyImmersiveMode() {
        getWindow().getDecorView().setSystemUiVisibility(
            View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY
                | View.SYSTEM_UI_FLAG_FULLSCREEN
                | View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                | View.SYSTEM_UI_FLAG_LAYOUT_STABLE);
    }
}
