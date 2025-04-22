#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <ctype.h> // For isspace, isdigit

// --- Configuration ---
// !!! IMPORTANT: This key MUST match the one in license_generator.c !!!
const char XOR_SECRET_KEY[] = "WigglesSecretCode!123"; // Keep this secret!

// --- Base64 Decoding ---
// Simple Base64 decoding adapted for this use case
const char b64chars[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

size_t b64_decoded_size(const char *in) {
    size_t len;
    size_t ret;
    size_t i;

    if (in == NULL)
        return 0;

    len = strlen(in);
    ret = len / 4 * 3;

    for (i=len; i-->0; ) {
        if (in[i] == '=') {
            ret--;
        } else {
            break;
        }
    }

    return ret;
}

int b64_isvalidchar(char c) {
    if (isalnum(c) || c == '+' || c == '/')
        return 1;
    return 0;
}

int b64_decode(const char *in, unsigned char *out, size_t outlen) {
    size_t len;
    size_t i;
    size_t j;
    int    v;

    if (in == NULL || out == NULL)
        return 0;

    len = strlen(in);
    if (outlen < b64_decoded_size(in) || len % 4 != 0)
        return 0;

    for (i=0; i<len; i++) {
        if (!b64_isvalidchar(in[i]) && in[i] != '=') {
            return 0;
        }
    }

    for (i=0, j=0; i<len; i+=4, j+=3) {
        v = strchr(b64chars, in[i]) - b64chars;
        v = v << 6 | (strchr(b64chars, in[i+1]) - b64chars);
        v = in[i+2]=='=' ? v << 6 : v << 6 | (strchr(b64chars, in[i+2]) - b64chars);
        v = in[i+3]=='=' ? v << 6 : v << 6 | (strchr(b64chars, in[i+3]) - b64chars);

        out[j] = (v >> 16) & 0xFF;
        if (in[i+2] != '=')
            out[j+1] = (v >> 8) & 0xFF;
        if (in[i+3] != '=')
            out[j+2] = v & 0xFF;
    }

    return 1;
}

// --- XOR Cipher ---
void xor_cipher(unsigned char *data, size_t data_len, const char *key, size_t key_len) {
     if (key_len == 0) return;
    for (size_t i = 0; i < data_len; i++) {
        data[i] = data[i] ^ key[i % key_len];
    }
}

// Function to format timestamp into a readable string
void format_time(time_t rawtime, char *buffer, size_t buffer_size) {
    struct tm *timeinfo = localtime(&rawtime);
    if (timeinfo != NULL) {
        strftime(buffer, buffer_size, "%Y-%m-%d %H:%M:%S", timeinfo);
    } else {
        snprintf(buffer, buffer_size, "<Invalid Time>");
    }
}

// Struct to hold decoded license info
typedef struct {
    char api_key[256]; // Adjust size as needed
    time_t expiry_time;
    int parse_success;
    int is_valid_now;
} LicenseInfo;

// Function to decode, decrypt (XOR), parse, and validate the license key
LicenseInfo validate_and_extract(const char *license_key) {
    LicenseInfo info = { {0}, 0, 0, 0 }; // Initialize
    size_t decoded_len;
    unsigned char *decoded_data = NULL;
    char *pipe_pos = NULL;

    // 1. Calculate required decoded length and allocate buffer
    decoded_len = b64_decoded_size(license_key);
    if (decoded_len == 0 || decoded_len >= 512) { // Add a sanity check size limit
        fprintf(stderr, "Error: Invalid Base64 format or potentially too long.\n");
        return info; // parse_success = 0
    }
    decoded_data = malloc(decoded_len + 1); // +1 for null terminator after XOR
    if (!decoded_data) {
        perror("Error: Failed to allocate memory for decoded data");
        return info;
    }

    // 2. Base64 Decode
    if (!b64_decode(license_key, decoded_data, decoded_len)) {
        fprintf(stderr, "Error: Base64 decoding failed. Invalid characters?\n");
        free(decoded_data);
        return info;
    }
    decoded_data[decoded_len] = '\0'; // Null terminate the raw decoded data before XOR

    // 3. XOR Decrypt
    xor_cipher(decoded_data, decoded_len, XOR_SECRET_KEY, strlen(XOR_SECRET_KEY));
    // decoded_data should now contain the original "API_KEY|EXPIRY_TIMESTAMP"
    printf("DEBUG: Data after XOR decryption: %s\n", decoded_data); // Debug

    // 4. Parse the decrypted string (API_KEY|EXPIRY_TIMESTAMP)
    pipe_pos = strchr((char*)decoded_data, '|');
    if (pipe_pos == NULL) {
        fprintf(stderr, "Error: Invalid format after decryption (missing '|'). Tampered key?\n");
        free(decoded_data);
        return info;
    }

    // Extract API Key part
    size_t api_key_len = pipe_pos - (char*)decoded_data;
    if (api_key_len >= sizeof(info.api_key)) {
        fprintf(stderr, "Error: Decoded API Key too long for buffer.\n");
        free(decoded_data);
        return info;
    }
    memcpy(info.api_key, decoded_data, api_key_len);
    info.api_key[api_key_len] = '\0'; // Null terminate API key

    // Extract and parse timestamp part
    char *timestamp_str = pipe_pos + 1;
    char *endptr;
    long expiry_timestamp_long = strtol(timestamp_str, &endptr, 10);
    if (timestamp_str == endptr || *endptr != '\0') {
        fprintf(stderr, "Error: Could not parse expiry timestamp after decryption. Tampered key?\n");
        free(decoded_data);
        return info;
    }
    info.expiry_time = (time_t)expiry_timestamp_long;
    info.parse_success = 1;

    // 5. Validate expiry time against current time
    time_t current_time = time(NULL);
    if (current_time == (time_t)-1) {
        perror("Error: Failed to get current system time for validation");
        info.is_valid_now = 0; // Treat as invalid if we can't get time
    } else {
        info.is_valid_now = (current_time <= info.expiry_time);
    }

    // Clean up
    free(decoded_data);
    return info;
}

// Helper function to remove trailing newline from fgets
void remove_newline(char *str) {
    size_t len = strlen(str);
    if (len > 0 && str[len - 1] == '\n') {
        str[len - 1] = '\0';
    }
}

void print_usage(const char *prog_name) {
    printf("========================================\n");
    printf("         Offline License Validator       \n");
    printf("========================================\n");
    printf("Usage: %s <license_key>\n", prog_name);
    printf("\n");
    printf("Example: %s 1234567890-1735689600\n", prog_name);
    printf("========================================\n");
}

int main(int argc, char *argv[]) {

    char license_key_input[512]; // Buffer for license key input
    char expiry_buf[30];
    char current_buf[30];

    // --- Wizard Start ---
    printf("========================================\n");
    printf("   🧙 Offline License Validator Wizard 🧙 \n");
    printf("   (XOR + Base64 Edition)             \n");
    printf("========================================\n");
    printf("Welcome! Let's check your license key.\n\n");

    // 1. Get License Key
    printf("🔑 Please enter the license key to validate: ");
    if (fgets(license_key_input, sizeof(license_key_input), stdin) == NULL) {
        fprintf(stderr, "\nError reading license key.\n");
        return 1;
    }
    remove_newline(license_key_input);

    // Basic check: ensure key is not empty
    if (strlen(license_key_input) == 0) {
         fprintf(stderr, "\nLicense key cannot be empty.\n");
         return 1;
    }

    // --- Validation ---
    printf("\n----------------------------------------\n");
    printf("    Validating Key...                   \n");
    printf("----------------------------------------\n");
    printf(" Key Provided: %s\n", license_key_input);
    printf("----------------------------------------\n");

    LicenseInfo info = validate_and_extract(license_key_input);

    // Format times
    if (info.parse_success) {
         format_time(info.expiry_time, expiry_buf, sizeof(expiry_buf));
    } else {
         snprintf(expiry_buf, sizeof(expiry_buf), "<N/A - Invalid Key Format>");
    }
    format_time(time(NULL), current_buf, sizeof(current_buf)); // Get current time again for display

    printf(" Parsed Expiry: %s\n", expiry_buf);
    printf(" Current Time:  %s\n", current_buf);

    int exit_code = 1; // Default to error/invalid

    if (info.parse_success) {
        if (info.is_valid_now) {
            printf("----------------------------------------\n");
            printf(" Result:        ✨ VALID ✨             \n");
            printf(" Extracted API Key: %s\n", info.api_key); // Show the key!
            printf("========================================\n");
            exit_code = 0; // Success
        } else {
            printf("----------------------------------------\n");
            printf(" Result:        ❌ EXPIRED ❌           \n");
             printf(" Extracted API Key: %s\n", info.api_key);
            printf("========================================\n");
            exit_code = 1; // Failure (Expired)
        }
    } else {
         printf("----------------------------------------\n");
        printf(" Result:        ❓ INVALID KEY FORMAT / ERROR ❓ \n");
        printf("========================================\n");
        exit_code = 2; // Failure (Invalid Format)
    }

    printf("Wizard finished. Have a wonderful day! :D\n");

    return exit_code;
} 