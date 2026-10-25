// Recherche vocale M3 (F3.7) : SFSpeechRecognizer + AVAudioEngine.
// Rappels vers Unity : OnNativeSpeechPartial / OnNativeSpeechResult / OnNativeSpeechError (M3VoiceSearch).
#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>
#import <Speech/Speech.h>

extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

static NSString* sTarget = nil;
static SFSpeechRecognizer* sRecognizer = nil;
static SFSpeechAudioBufferRecognitionRequest* sRequest = nil;
static SFSpeechRecognitionTask* sTask = nil;
static AVAudioEngine* sEngine = nil;
static BOOL sFinished = NO;

static void DecoARSend(const char* method, NSString* text)
{
    if (sTarget == nil) return;
    UnitySendMessage([sTarget UTF8String], method, text != nil ? [text UTF8String] : "");
}

static void DecoARStopAudio()
{
    if (sEngine != nil && sEngine.isRunning)
    {
        [sEngine stop];
        [[sEngine inputNode] removeTapOnBus:0];
    }
    if (sRequest != nil) [sRequest endAudio];
    [[AVAudioSession sharedInstance] setActive:NO withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation error:nil];
}

static void DecoARFinish(const char* method, NSString* text)
{
    if (sFinished) return;
    sFinished = YES;
    DecoARStopAudio();
    if (sTask != nil) { [sTask cancel]; sTask = nil; }
    sRequest = nil;
    DecoARSend(method, text);
}

static void DecoARBeginRecognition()
{
    NSError* error = nil;
    AVAudioSession* session = [AVAudioSession sharedInstance];
    [session setCategory:AVAudioSessionCategoryPlayAndRecord mode:AVAudioSessionModeMeasurement
                 options:AVAudioSessionCategoryOptionDuckOthers | AVAudioSessionCategoryOptionDefaultToSpeaker error:&error];
    [session setActive:YES withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation error:&error];
    if (error != nil) { DecoARFinish("OnNativeSpeechError", @"audio"); return; }

    if (sRecognizer == nil || !sRecognizer.isAvailable) { DecoARFinish("OnNativeSpeechError", @"unavailable"); return; }

    sEngine = [[AVAudioEngine alloc] init];
    sRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    sRequest.shouldReportPartialResults = YES;

    AVAudioInputNode* input = [sEngine inputNode];
    AVAudioFormat* format = [input outputFormatForBus:0];
    SFSpeechAudioBufferRecognitionRequest* request = sRequest;
    [input installTapOnBus:0 bufferSize:1024 format:format block:^(AVAudioPCMBuffer* buffer, AVAudioTime* when) {
        [request appendAudioPCMBuffer:buffer];
    }];

    [sEngine prepare];
    if (![sEngine startAndReturnError:&error]) { DecoARFinish("OnNativeSpeechError", @"audio"); return; }

    sTask = [sRecognizer recognitionTaskWithRequest:sRequest resultHandler:^(SFSpeechRecognitionResult* result, NSError* err) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (sFinished) return;
            if (result != nil)
            {
                NSString* text = result.bestTranscription.formattedString;
                if (result.isFinal) { DecoARFinish("OnNativeSpeechResult", text); return; }
                DecoARSend("OnNativeSpeechPartial", text);
            }
            if (err != nil) DecoARFinish("OnNativeSpeechResult", @"");
        });
    }];
}

extern "C" {

void _DecoARM3_StartSpeech(const char* gameObjectName, const char* locale)
{
    sTarget = [NSString stringWithUTF8String:gameObjectName];
    sFinished = NO;
    NSString* loc = locale != NULL ? [NSString stringWithUTF8String:locale] : @"fr-FR";
    sRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:loc]];

    // Autorisations demandees au moment de l'usage (textes dans Info.plist)
    [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
        dispatch_async(dispatch_get_main_queue(), ^{
            if (status != SFSpeechRecognizerAuthorizationStatusAuthorized) { DecoARFinish("OnNativeSpeechError", @"denied"); return; }
            [[AVAudioSession sharedInstance] requestRecordPermission:^(BOOL granted) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (!granted) { DecoARFinish("OnNativeSpeechError", @"denied"); return; }
                    DecoARBeginRecognition();
                });
            }];
        });
    }];
}

void _DecoARM3_StopSpeech()
{
    // Fin de l'audio : le recognizer renvoie alors son resultat final
    DecoARStopAudio();
}

}
