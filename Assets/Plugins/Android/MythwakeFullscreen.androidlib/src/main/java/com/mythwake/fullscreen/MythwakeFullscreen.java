package com.mythwake.fullscreen;

import android.app.Activity;
import android.os.Build;
import android.view.View;
import android.view.Window;
import android.view.WindowInsets;
import android.view.WindowInsetsController;
import android.view.WindowManager;

public final class MythwakeFullscreen {
    private static final long[] REAPPLY_DELAYS_MS = new long[] { 120L, 300L, 750L, 1500L, 3000L };

    private MythwakeFullscreen() {
    }

    public static void apply(final Activity activity) {
        if (activity == null) {
            return;
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                applyNow(activity);
                installListeners(activity);
                scheduleReapply(activity);
            }
        });
    }

    private static void installListeners(final Activity activity) {
        final View decorView = getDecorView(activity);
        if (decorView == null) {
            return;
        }

        decorView.setOnSystemUiVisibilityChangeListener(new View.OnSystemUiVisibilityChangeListener() {
            @Override
            public void onSystemUiVisibilityChange(int visibility) {
                if ((visibility & View.SYSTEM_UI_FLAG_FULLSCREEN) == 0) {
                    postApply(activity, decorView, 120L);
                }
            }
        });

        decorView.setOnApplyWindowInsetsListener(new View.OnApplyWindowInsetsListener() {
            @Override
            public WindowInsets onApplyWindowInsets(View view, WindowInsets insets) {
                postApply(activity, view, 120L);
                return insets;
            }
        });
    }

    private static void scheduleReapply(final Activity activity) {
        View decorView = getDecorView(activity);
        if (decorView == null) {
            return;
        }

        for (long delayMs : REAPPLY_DELAYS_MS) {
            postApply(activity, decorView, delayMs);
        }
    }

    private static void postApply(final Activity activity, View view, long delayMs) {
        view.postDelayed(new Runnable() {
            @Override
            public void run() {
                applyNow(activity);
            }
        }, delayMs);
    }

    private static View getDecorView(Activity activity) {
        Window window = activity.getWindow();
        if (window == null) {
            return null;
        }

        return window.getDecorView();
    }

    private static void applyNow(Activity activity) {
        Window window = activity.getWindow();
        if (window == null) {
            return;
        }

        window.addFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
        window.setFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN, WindowManager.LayoutParams.FLAG_FULLSCREEN);
        window.clearFlags(WindowManager.LayoutParams.FLAG_FORCE_NOT_FULLSCREEN);

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            window.setStatusBarColor(0x00000000);
            window.setNavigationBarColor(0x00000000);
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            WindowManager.LayoutParams attributes = window.getAttributes();
            attributes.layoutInDisplayCutoutMode = WindowManager.LayoutParams.LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES;
            window.setAttributes(attributes);
        }

        View decorView = window.getDecorView();
        if (decorView != null) {
            decorView.setSystemUiVisibility(
                View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY |
                View.SYSTEM_UI_FLAG_FULLSCREEN |
                View.SYSTEM_UI_FLAG_HIDE_NAVIGATION |
                View.SYSTEM_UI_FLAG_LAYOUT_STABLE |
                View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN |
                View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
            );
        }

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
            window.setDecorFitsSystemWindows(false);
            WindowInsetsController controller = window.getInsetsController();
            if (controller != null) {
                controller.setSystemBarsBehavior(WindowInsetsController.BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE);
                controller.hide(WindowInsets.Type.statusBars() | WindowInsets.Type.navigationBars());
            }
        }
    }
}
