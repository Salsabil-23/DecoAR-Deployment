#import <UIKit/UIKit.h>

extern "C" {
    void _DecoARM4_Impact(int style)
    {
        UIImpactFeedbackStyle s = UIImpactFeedbackStyleMedium;
        if (style == 0) s = UIImpactFeedbackStyleLight;
        else if (style == 2) s = UIImpactFeedbackStyleHeavy;

        dispatch_async(dispatch_get_main_queue(), ^{
            UIImpactFeedbackGenerator *g = [[UIImpactFeedbackGenerator alloc] initWithStyle:s];
            [g prepare];
            [g impactOccurred];
        });
    }
}