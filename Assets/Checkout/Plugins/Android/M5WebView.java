package com.decoar.m5;

import android.app.Activity;
import android.app.Dialog;
import android.content.DialogInterface;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.view.WindowManager;
import android.webkit.WebResourceRequest;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.Button;
import android.widget.LinearLayout;
import android.widget.ProgressBar;
import android.widget.TextView;

import com.unity3d.player.UnityPlayer;

/**
 * Page web plein écran DANS l'application (paiement Stripe de la caisse DecoAR M5).
 * Quand la page atteint doneUrlPrefix (retour Stripe), la fenêtre se ferme et Unity reçoit
 * gameObject.OnWebViewResult(url). Fermer / bouton retour envoie "closed".
 */
public class M5WebView {
    private static Dialog dialog;
    private static boolean done;

    public static void open(final String url, final String doneUrlPrefix, final String gameObject) {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                dismiss();
                done = false;

                final WebView web = new WebView(activity);
                WebSettings s = web.getSettings();
                s.setJavaScriptEnabled(true);
                s.setDomStorageEnabled(true);
                s.setSupportMultipleWindows(false);

                final ProgressBar progress = new ProgressBar(activity, null, android.R.attr.progressBarStyleHorizontal);
                progress.setIndeterminate(true);

                web.setWebViewClient(new WebViewClient() {
                    @Override public boolean shouldOverrideUrlLoading(WebView view, WebResourceRequest request) {
                        return check(request.getUrl().toString());
                    }
                    @Override public void onPageStarted(WebView view, String u, Bitmap favicon) {
                        progress.setVisibility(View.VISIBLE);
                        if (check(u)) view.stopLoading();
                    }
                    @Override public void onPageFinished(WebView view, String u) {
                        progress.setVisibility(View.GONE);
                    }
                    private boolean check(String u) {
                        if (u == null || !u.startsWith(doneUrlPrefix)) return false;
                        finish(gameObject, u);
                        return true;
                    }
                });

                // Barre du haut : titre + Fermer
                LinearLayout bar = new LinearLayout(activity);
                bar.setOrientation(LinearLayout.HORIZONTAL);
                bar.setGravity(Gravity.CENTER_VERTICAL);
                bar.setBackgroundColor(Color.rgb(43, 36, 28));
                int pad = dp(activity, 10);
                bar.setPadding(pad * 2, pad, pad, pad);
                TextView title = new TextView(activity);
                title.setText("Paiement sécurisé · Stripe");
                title.setTextColor(Color.rgb(250, 242, 227));
                title.setTextSize(17);
                bar.addView(title, new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WRAP_CONTENT, 1f));
                Button close = new Button(activity);
                close.setText("Fermer");
                close.setOnClickListener(new View.OnClickListener() {
                    @Override public void onClick(View v) { finish(gameObject, "closed"); }
                });
                bar.addView(close);

                LinearLayout root = new LinearLayout(activity);
                root.setOrientation(LinearLayout.VERTICAL);
                root.setBackgroundColor(Color.WHITE);
                root.addView(bar, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, ViewGroup.LayoutParams.WRAP_CONTENT));
                root.addView(progress, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, dp(activity, 4)));
                root.addView(web, new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MATCH_PARENT, 0, 1f));

                dialog = new Dialog(activity, android.R.style.Theme_Black_NoTitleBar_Fullscreen);
                dialog.setContentView(root);
                dialog.getWindow().setSoftInputMode(WindowManager.LayoutParams.SOFT_INPUT_ADJUST_RESIZE);
                dialog.setOnCancelListener(new DialogInterface.OnCancelListener() { // bouton retour
                    @Override public void onCancel(DialogInterface d) { finish(gameObject, "closed"); }
                });
                dialog.show();
                web.loadUrl(url);
            }
        });
    }

    /** Fermeture demandée par Unity (ex. paiement confirmé par le serveur). */
    public static void close() {
        final Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override public void run() { done = true; dismiss(); }
        });
    }

    private static void finish(String gameObject, String result) {
        if (done) return;
        done = true;
        UnityPlayer.UnitySendMessage(gameObject, "OnWebViewResult", result);
        dismiss();
    }

    private static void dismiss() {
        if (dialog != null) { dialog.dismiss(); dialog = null; }
    }

    private static int dp(Activity a, int v) {
        return Math.round(v * a.getResources().getDisplayMetrics().density);
    }
}
