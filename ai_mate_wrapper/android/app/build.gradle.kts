import java.util.Properties
import java.io.FileInputStream

plugins {
    id("com.android.application")
    id("kotlin-android")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
    // Gradle Play Publisher for scripted uploads to Google Play
    id("com.github.triplet.play") version "3.8.6"
}

// Attempt to load signing config from key.properties if available
val keystoreProperties = Properties()
// Prefer repo-root key.properties if present, otherwise fallback to android/ key.properties
val repoRootKeyProps = rootProject.file("../../key.properties")
val androidKeyProps = rootProject.file("key.properties")
val keystorePropertiesFile = if (repoRootKeyProps.exists()) repoRootKeyProps else androidKeyProps
if (keystorePropertiesFile.exists()) {
    FileInputStream(keystorePropertiesFile).use { fis ->
        keystoreProperties.load(fis)
    }
}

android {
    namespace = "com.fiftyeightsoftware.aimate"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }
    kotlinOptions {
        jvmTarget = JavaVersion.VERSION_11.toString()
    }

    defaultConfig {
        // TODO: Specify your own unique Application ID (https://developer.android.com/studio/build/application-id.html).
        applicationId = "com.fiftyeightsoftware.aimate"
        // You can update the following values to match your application needs.
        // For more information, see: https://flutter.dev/to/review-gradle-config.
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    signingConfigs {
        if (keystoreProperties.isNotEmpty()) {
            create("release") {
                val storeFileProp = keystoreProperties["storeFile"] as String
                fun resolveKeystorePath(path: String): File {
                    val direct = file(path)
                    if (direct.exists()) return direct

                    val base = File(path).name

                    // Try relative to android/ (rootProject)
                    val androidRel = rootProject.file(path)
                    if (androidRel.exists()) return androidRel

                    // Try repo root (two levels up from android/)
                    val repoRootRel = rootProject.file("../../$path")
                    if (repoRootRel.exists()) return repoRootRel

                    // Try one level up from android/ just in case
                    val oneUp = rootProject.file("../$path")
                    if (oneUp.exists()) return oneUp

                    // Try basename in app/ directory
                    val appBase = file(base)
                    if (appBase.exists()) return appBase

                    // Fallback to direct
                    return direct
                }
                storePassword = keystoreProperties["storePassword"] as String
                keyAlias = keystoreProperties["keyAlias"] as String
                keyPassword = keystoreProperties["keyPassword"] as String
            }
        }
    }
flutter {
    source = "../.."
    val androidCred = rootProject.file("../play-service-account.json")
    val credFile = if (rootCred.exists()) rootCred else androidCred
    serviceAccountCredentials.set(credFile)
    // Upload to the Internal testing track
    track.set("internal")
    // Always prefer App Bundles
    defaultToAppBundles.set(true)
    // Mark release as completed on upload
    releaseStatus.set(com.github.triplet.gradle.androidpublisher.ReleaseStatus.COMPLETED)
}
