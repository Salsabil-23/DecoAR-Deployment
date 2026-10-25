// Page web plein écran DANS l'application (paiement Stripe de la caisse DecoAR M5).
// Quand la page atteint doneUrlPrefix (retour Stripe), la fenêtre se ferme et Unity reçoit
// gameObject.OnWebViewResult(url). Le bouton Fermer envoie "closed".
#import <UIKit/UIKit.h>
#import <WebKit/WebKit.h>

extern "C" void UnitySendMessage(const char* obj, const char* method, const char* msg);

@interface M5WebViewController : UIViewController <WKNavigationDelegate>
@property (nonatomic, copy) NSString* startUrl;
@property (nonatomic, copy) NSString* doneUrl;
@property (nonatomic, copy) NSString* gameObject;
@property (nonatomic, strong) WKWebView* web;
@property (nonatomic, strong) UIActivityIndicatorView* spinner;
@property (nonatomic) BOOL done;
@end

static M5WebViewController* s_current = nil;

@implementation M5WebViewController

- (void)viewDidLoad
{
    [super viewDidLoad];
    self.view.backgroundColor = UIColor.whiteColor;

    UIView* bar = [[UIView alloc] init];
    bar.translatesAutoresizingMaskIntoConstraints = NO;
    bar.backgroundColor = [UIColor colorWithRed:43/255.0 green:36/255.0 blue:28/255.0 alpha:1];
    [self.view addSubview:bar];

    UILabel* title = [[UILabel alloc] init];
    title.translatesAutoresizingMaskIntoConstraints = NO;
    title.text = @"Paiement sécurisé · Stripe";
    title.textColor = [UIColor colorWithRed:250/255.0 green:242/255.0 blue:227/255.0 alpha:1];
    title.font = [UIFont boldSystemFontOfSize:17];
    [bar addSubview:title];

    UIButton* close = [UIButton buttonWithType:UIButtonTypeSystem];
    close.translatesAutoresizingMaskIntoConstraints = NO;
    [close setTitle:@"Fermer" forState:UIControlStateNormal];
    close.titleLabel.font = [UIFont systemFontOfSize:17];
    [close addTarget:self action:@selector(onClose) forControlEvents:UIControlEventTouchUpInside];
    [bar addSubview:close];

    self.web = [[WKWebView alloc] initWithFrame:CGRectZero configuration:[[WKWebViewConfiguration alloc] init]];
    self.web.translatesAutoresizingMaskIntoConstraints = NO;
    self.web.navigationDelegate = self;
    [self.view addSubview:self.web];

    self.spinner = [[UIActivityIndicatorView alloc] initWithActivityIndicatorStyle:UIActivityIndicatorViewStyleMedium];
    self.spinner.translatesAutoresizingMaskIntoConstraints = NO;
    self.spinner.hidesWhenStopped = YES;
    [self.view addSubview:self.spinner];

    UILayoutGuide* safe = self.view.safeAreaLayoutGuide;
    [NSLayoutConstraint activateConstraints:@[
        [bar.topAnchor constraintEqualToAnchor:self.view.topAnchor],
        [bar.leadingAnchor constraintEqualToAnchor:self.view.leadingAnchor],
        [bar.trailingAnchor constraintEqualToAnchor:self.view.trailingAnchor],
        [bar.bottomAnchor constraintEqualToAnchor:safe.topAnchor constant:50],
        [title.leadingAnchor constraintEqualToAnchor:safe.leadingAnchor constant:20],
        [title.bottomAnchor constraintEqualToAnchor:bar.bottomAnchor constant:-14],
        [close.trailingAnchor constraintEqualToAnchor:safe.trailingAnchor constant:-16],
        [close.centerYAnchor constraintEqualToAnchor:title.centerYAnchor],
        [self.web.topAnchor constraintEqualToAnchor:bar.bottomAnchor],
        [self.web.leadingAnchor constraintEqualToAnchor:self.view.leadingAnchor],
        [self.web.trailingAnchor constraintEqualToAnchor:self.view.trailingAnchor],
        [self.web.bottomAnchor constraintEqualToAnchor:self.view.bottomAnchor],
        [self.spinner.centerXAnchor constraintEqualToAnchor:self.web.centerXAnchor],
        [self.spinner.centerYAnchor constraintEqualToAnchor:self.web.centerYAnchor],
    ]];

    [self.web loadRequest:[NSURLRequest requestWithURL:[NSURL URLWithString:self.startUrl]]];
}

- (void)webView:(WKWebView*)webView decidePolicyForNavigationAction:(WKNavigationAction*)action
    decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    NSString* url = action.request.URL.absoluteString;
    if (url && [url hasPrefix:self.doneUrl])
    {
        decisionHandler(WKNavigationActionPolicyCancel);
        [self finish:url];
        return;
    }
    decisionHandler(WKNavigationActionPolicyAllow);
}

- (void)webView:(WKWebView*)webView didStartProvisionalNavigation:(WKNavigation*)navigation { [self.spinner startAnimating]; }
- (void)webView:(WKWebView*)webView didFinishNavigation:(WKNavigation*)navigation { [self.spinner stopAnimating]; }

- (void)onClose { [self finish:@"closed"]; }

- (void)finish:(NSString*)result
{
    if (self.done) return;
    self.done = YES;
    UnitySendMessage(self.gameObject.UTF8String, "OnWebViewResult", result.UTF8String);
    [self dismissViewControllerAnimated:YES completion:nil];
    if (s_current == self) s_current = nil;
}

@end

static UIViewController* M5TopViewController()
{
    UIViewController* vc = nil;
    for (UIScene* scene in UIApplication.sharedApplication.connectedScenes)
        if ([scene isKindOfClass:UIWindowScene.class])
            for (UIWindow* w in ((UIWindowScene*)scene).windows)
                if (w.isKeyWindow) vc = w.rootViewController;
    while (vc.presentedViewController) vc = vc.presentedViewController;
    return vc;
}

extern "C" void _M5WebView_Open(const char* url, const char* doneUrl, const char* gameObject)
{
    NSString* u = [NSString stringWithUTF8String:url];
    NSString* d = [NSString stringWithUTF8String:doneUrl];
    NSString* g = [NSString stringWithUTF8String:gameObject];
    dispatch_async(dispatch_get_main_queue(), ^{
        if (s_current) { s_current.done = YES; [s_current dismissViewControllerAnimated:NO completion:nil]; s_current = nil; }
        M5WebViewController* vc = [[M5WebViewController alloc] init];
        vc.startUrl = u; vc.doneUrl = d; vc.gameObject = g;
        vc.modalPresentationStyle = UIModalPresentationFullScreen;
        s_current = vc;
        [M5TopViewController() presentViewController:vc animated:YES completion:nil];
    });
}

extern "C" void _M5WebView_Close()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        if (!s_current) return;
        s_current.done = YES;
        [s_current dismissViewControllerAnimated:YES completion:nil];
        s_current = nil;
    });
}
