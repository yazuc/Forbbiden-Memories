import json
import re

def convert_js_to_json(input_filename, output_filename):
    try:
        with open(input_filename, 'r', encoding='utf-8') as f:
            content = f.read()

        # 1. Isolate the array: Grab everything from the first '[' to the last ']'
        start = content.find('[')
        end = content.rfind(']') + 1
        if start == -1 or end == 0:
            print("Could not find the start or end of the data array.")
            return
            
        raw_data = content[start:end]

        # 2. Key-Standardization: Wrap unquoted keys in quotes
        # This turns Name: into "Name": while leaving already quoted keys alone.
        raw_data = re.sub(r'([{,]\s*)(\b\w+|_\w+)\s*:', r'\1"\2":', raw_data)

        # 3. State Machine: Normalize all strings and handle internal quotes/escapes
        final_chars = []
        in_string = False
        quote_type = None  # Tracks if we are in a '...' or "..." string
        
        i = 0
        while i < len(raw_data):
            char = raw_data[i]
            
            if not in_string:
                if char in ("'", '"'):
                    in_string = True
                    quote_type = char
                    final_chars.append('"') # Standardize to double quotes
                else:
                    final_chars.append(char)
            else:
                # We are INSIDE a string
                if char == quote_type:
                    # Potential end of string. Verify if it's structural (followed by : , } or ])
                    lookahead = raw_data[i+1:i+15].strip()
                    if lookahead.startswith((':')) or lookahead.startswith((',', '}', ']')):
                        in_string = False
                        final_chars.append('"')
                    else:
                        # Rogue quote inside text (e.g., King of the "Sea" or that's)
                        final_chars.append('\\"' if char == '"' else "'")
                elif char == '"' and quote_type == "'":
                    # Found a double quote inside a single-quoted string
                    final_chars.append('\\"')
                elif char == '\\':
                    # Handle escapes
                    if i + 1 < len(raw_data):
                        next_char = raw_data[i+1]
                        if next_char == "'" and quote_type == "'":
                            # Fix the specific \' problem: convert it to a regular '
                            final_chars.append("'")
                        elif next_char == '"':
                            final_chars.append('\\"')
                        elif next_char in ('r', 'n', 't', '\\', '/'):
                            # Keep standard JSON escapes
                            final_chars.append('\\' + next_char)
                        i += 1
                else:
                    # Clean up illegal control characters for JSON (tabs/newlines)
                    if char == '\n' or char == '\r' or char == '\t':
                        final_chars.append(' ')
                    else:
                        final_chars.append(char)
            i += 1

        cleaned_str = "".join(final_chars)

        # 4. Final Cleanup
        cleaned_str = cleaned_str.replace(': null', ': null')
        cleaned_str = re.sub(r',\s*([\]}])', r'\1', cleaned_str) # Remove trailing commas

        # 5. Parse and Export
        data = json.loads(cleaned_str, strict=False)

        with open(output_filename, 'w', encoding='utf-8') as f:
            json.dump(data, f, indent=2, ensure_ascii=False)

        print(f"✅ Success! {len(data)} cards converted to {output_filename}")

    except Exception as e:
        print(f"❌ Conversion failed. Error: {e}")
        if 'cleaned_str' in locals() and hasattr(e, 'pos'):
            context = cleaned_str[max(0, e.pos-50):e.pos+50]
            print(f"Error Context around char {e.pos}:\n{'-'*30}\n{context}\n{'-'*30}")

convert_js_to_json('cards.js', 'cards.json')
