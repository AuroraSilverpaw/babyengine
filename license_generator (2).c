#define _XOPEN_SOURCE
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <ctype.h> // For isdigit

// --- Configuration ---
// !!! IMPORTANT: This key MUST match the one in license_validator.c !!!
const char XOR_SECRET_KEY[] = "WigglesSecretCode!123"; // Keep this secret!

// --- Base64 Encoding ---
// Simple Base64 encoding adapted for this use case
const char b64chars[] = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

size_t b64_encoded_size(size_t inlen) {
    size_t ret;
    ret = inlen;
    if (inlen % 3 != 0)
        ret += 3 - (inlen % 3);
    ret /= 3;
    ret *= 4;
    ret += 1; // For null terminator
    return ret;
}

char *b64_encode(const unsigned char *in, size_t len) {
    char   *out;
    size_t  elen;
    size_t  i;
    size_t  j;
    size_t  v;

    if (in == NULL || len == 0)
        return NULL;

    elen = b64_encoded_size(len);
    out  = malloc(elen);
    if (out == NULL)
        return NULL;
    out[elen - 1] = '\0'; // Null terminate

    for (i=0, j=0; i<len; i+=3, j+=4) {
        v = in[i];
        v = i+1 < len ? v << 8 | in[i+1] : v << 8;
        v = i+2 < len ? v << 8 | in[i+2] : v << 8;

        out[j]   = b64chars[(v >> 18) & 0x3F];
        out[j+1] = b64chars[(v >> 12) & 0x3F];
        if (i+1 < len) {
            out[j+2] = b64chars[(v >> 6) & 0x3F];
        } else {
            out[j+2] = '=';
        }
        if (i+2 < len) {
            out[j+3] = b64chars[v & 0x3F];
        } else {
            out[j+3] = '=';
        }
    }

    return out;
}

// --- XOR Cipher ---
void xor_cipher(unsigned char *data, size_t data_len, const char *key, size_t key_len) {
    if (key_len == 0) return; // No key, no cipher
    for (size_t i = 0; i < data_len; i++) {
        data[i] = data[i] ^ key[i % key_len];
    }
}


// --- License Generation Logic ---

// Function to generate the license key (Now XORed and Base64 encoded)
char* generate_license_key(const char *api_key, const char *start_date_str, int duration_days) {
    // 1. Calculate expiry timestamp
    struct tm start_tm = {0};
    time_t start_time;
    time_t expiry_time;

    if (strptime(start_date_str, "%Y-%m-%d", &start_tm) == NULL) {
        fprintf(stderr, "Error parsing start date inside generate_license_key.\n");
        return NULL; // Should have been caught earlier, but double check
    }
    start_tm.tm_isdst = -1;
    start_time = mktime(&start_tm);
    if (start_time == -1) {
        fprintf(stderr, "Error converting start date to time_t.\n");
        return NULL;
    }
    expiry_time = start_time + (duration_days * 24 * 60 * 60);

    // 2. Create plain data string: API_KEY|EXPIRY_TIMESTAMP
    char expiry_str[20]; // Enough for a 64-bit timestamp
    snprintf(expiry_str, sizeof(expiry_str), "%ld", (long)expiry_time);

    size_t plain_data_len = strlen(api_key) + 1 + strlen(expiry_str);
    unsigned char *plain_data = malloc(plain_data_len + 1); // +1 for null terminator
    if (!plain_data) {
        perror("Failed to allocate memory for plain data");
        return NULL;
    }
    snprintf((char*)plain_data, plain_data_len + 1, "%s|%s", api_key, expiry_str);

    // 3. XOR Cipher the plain data
    printf("DEBUG: Plain data before XOR: %s\n", plain_data); // Debug
    xor_cipher(plain_data, plain_data_len, XOR_SECRET_KEY, strlen(XOR_SECRET_KEY));
    // Note: plain_data now contains non-printable characters after XOR

    // 4. Base64 Encode the XORed data
    char *license_key = b64_encode(plain_data, plain_data_len);

    // 5. Clean up temporary data
    free(plain_data);

    if (license_key == NULL) {
         fprintf(stderr, "Error during Base64 encoding.\n");
         return NULL;
    }

    return license_key;
}

// --- Helper functions (remove_newline, is_valid_date_format, read_positive_integer) remain the same ---
// Helper function to remove trailing newline from fgets
void remove_newline(char *str) {
    size_t len = strlen(str);
    if (len > 0 && str[len - 1] == '\n') {
        str[len - 1] = '\0';
    }
}

// Helper function to validate YYYY-MM-DD format
int is_valid_date_format(const char *date_str) {
    if (strlen(date_str) != 10) return 0;
    for (int i = 0; i < 10; i++) {
        if (i == 4 || i == 7) {
            if (date_str[i] != '-') return 0;
        } else {
            if (!isdigit(date_str[i])) return 0;
        }
    }
    // Basic format check passed, strptime will do the rest
    struct tm test_tm;
    return (strptime(date_str, "%Y-%m-%d", &test_tm) != NULL);
}

// Helper function to validate positive integer input
int read_positive_integer(const char *prompt) {
    char input_buffer[32];
    int number = 0;
    int valid_input = 0;

    while (!valid_input) {
        printf("%s", prompt);
        if (fgets(input_buffer, sizeof(input_buffer), stdin) == NULL) {
            fprintf(stderr, "Error reading input.\n");
            return -1; // Indicate error
        }
        remove_newline(input_buffer);

        // Check if input is a valid positive integer
        char *endptr;
        long val = strtol(input_buffer, &endptr, 10);

        if (endptr == input_buffer || *endptr != '\0' || val <= 0 || val > 2147483647) { // Check for non-numeric, empty, non-positive, or overflow
            printf("Invalid input. Please enter a positive whole number.\n");
        } else {
            number = (int)val;
            valid_input = 1;
        }
    }
    return number;
}


// --- Main Wizard Logic (mostly the same, calls new generate_license_key) ---
int main(int argc, char *argv[]) {
    char api_key[256]; // Buffer for API key
    char start_date_str[11]; // YYYY-MM-DD + null terminator
    int duration_days = 0;

    // --- Wizard Start ---\n
    printf("========================================\n");
    printf("   🧙 Offline License Generator Wizard 🧙 \n");
    printf("   (XOR + Base64 Edition)             \n");
    printf("========================================\n");
    printf("Welcome! Let's generate a license key.\n\n");

    // 1. Get API Key
    printf("🔑 Please enter the API Key: ");
    if (fgets(api_key, sizeof(api_key), stdin) == NULL) {
        fprintf(stderr, "\nError reading API Key.\n");
        return 1;
    }
    remove_newline(api_key);
    if (strlen(api_key) == 0) {
         fprintf(stderr, "\nAPI Key cannot be empty.\n");
         return 1;
    }


    // 2. Get Start Date
    int valid_date = 0;
    while (!valid_date) {
        printf("📅 Enter the license start date (YYYY-MM-DD): ");
        if (fgets(start_date_str, sizeof(start_date_str), stdin) == NULL) {
             fprintf(stderr, "\nError reading start date.\n");
             return 1;
        }
        remove_newline(start_date_str);

        // Consume potential extra input if user entered more than 10 chars
        if (strchr(start_date_str, '\n') == NULL && strlen(start_date_str) == sizeof(start_date_str) -1) {
            int c;
            while ((c = getchar()) != '\n' && c != EOF);
        }

        if (is_valid_date_format(start_date_str)) {
            valid_date = 1;
        } else {
            printf("Invalid date format or date. Please use YYYY-MM-DD format (e.g., 2024-07-26).\n");
        }
    }

    // 3. Get Duration
    duration_days = read_positive_integer("⏳ Enter the license duration (in days): ");
    if (duration_days == -1) { // Check for error from read_positive_integer
        return 1;
    }


    // --- Generation ---\n
    printf("\n----------------------------------------\n");
    printf("   Generating with the following info:    \n");
    printf("----------------------------------------\n");
    printf(" API Key:      *** HIDDEN ***\n"); // Still hide it!
    printf(" Start Date:   %s\n", start_date_str);
    printf(" Duration:     %d days\n", duration_days);
    printf("----------------------------------------\n");

    char *license = generate_license_key(api_key, start_date_str, duration_days);

    if (license != NULL) {
        printf("✨ License Key Generated Successfully! ✨\n");
        printf(" >> %s <<\n", license);
        printf("========================================\n");
        free(license);
    } else {
        fprintf(stderr, "\n--- Failed to generate license key! :( ---\n");
        return 1;
    }

    printf("Wizard finished. Have a magical day! ^.^\n");
    return 0; // Indicate success
} 