# Notification System Rules - Technical Specification

## Email & Snack Notifications

### Delivery Rules
- **Single delivery**: Sent once and immediately marked as sent
- **Scheduled delivery**: Must have a specific time to be sent
- **No end date**: Cannot have an end date configured
- **Non-continuous**: Cannot be set as continuous notifications

### Trigger Behavior
- **On start**: Send email immediately when notification starts
- **User logged in**: Send push notification if user is logged in when notification starts
- **Firebase integration**: Send to Firebase but do NOT mark as read (frontend must handle marking as read)

### Visibility Rules
- **Email**: Not available in `GetUnvisualizedNotifications` endpoint (called by frontend on startup or notification loading)
- **Push**: Automatically marked as visualized in `GetUnvisualizedNotifications`

## Push & Message Notifications

### Delivery Rules
- **End date support**: Can have an end date for delivery
- **Continuous support**: Messages can be set as continuous notifications
- **Trigger condition**: Send when start date is reached, if user is logged in OR send to Firebase

### Visibility Rules
- **Availability**: Available in `GetUnvisualizedNotifications` but NOT automatically marked as visualized
- **Push notifications**: Must wait for user to click or clear to mark as visualized
- **Messages (non-continuous)**: Marked as visualized when user clicks "OK"
- **Messages (continuous)**: Marked as visualized only when user clicks "Don't show again"
- **Message Clicked**: open the message on screen
- **Messages**: message can be removed, than mark as read

### UI Behavior
- **Side menu**: Both message and push notifications remain in side menu until marked as visualized
- **Screen display**: Messages appear on screen when received via notification hub

## API Endpoint Specifications

### GetUnvisualizedNotifications
- **Includes**: Push notifications, Message notifications, Snack notifications
- **Excludes**: Email notifications
- **Auto-marking**: Only Push notifications are automatically marked as visualized
- **Manual marking**: Message asn push notifications require user interaction to be marked as visualized