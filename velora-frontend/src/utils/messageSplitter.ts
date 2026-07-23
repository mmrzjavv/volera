/**
 * Splits a long message into multiple messages that respect the maximum length limit.
 * Attempts to split at word boundaries when possible to maintain readability.
 * 
 * @param message - The message content to split
 * @param maxLength - Maximum length per message chunk
 * @returns Array of message chunks
 */
export function splitMessage(message: string, maxLength: number): string[] {
  if (message.length <= maxLength) {
    return [message];
  }

  const chunks: string[] = [];
  let remaining = message;

  while (remaining.length > 0) {
    if (remaining.length <= maxLength) {
      chunks.push(remaining);
      break;
    }

    // Try to find a good split point (prefer word boundaries)
    let splitIndex = maxLength;
    
    // Look for a space, newline, or punctuation near the max length
    const searchStart = Math.max(0, maxLength - 100); // Look back up to 100 chars
    
    // Prefer splitting at newlines
    let newlineIndex = remaining.lastIndexOf('\n', maxLength);
    if (newlineIndex > searchStart && newlineIndex <= maxLength) {
      splitIndex = newlineIndex + 1; // Include the newline
    } else {
      // Look for spaces or punctuation
      const spaceIndex = remaining.lastIndexOf(' ', maxLength);
      const periodIndex = remaining.lastIndexOf('.', maxLength);
      const commaIndex = remaining.lastIndexOf(',', maxLength);
      
      // Prefer period > comma > space > hard cut
      if (periodIndex > searchStart && periodIndex <= maxLength) {
        splitIndex = periodIndex + 1;
      } else if (commaIndex > searchStart && commaIndex <= maxLength) {
        splitIndex = commaIndex + 1;
      } else if (spaceIndex > searchStart && spaceIndex <= maxLength) {
        splitIndex = spaceIndex + 1;
      }
      // If no good split point found, split at maxLength (hard cut)
    }

    const chunk = remaining.substring(0, splitIndex).trim();
    if (chunk.length > 0) {
      chunks.push(chunk);
    }
    remaining = remaining.substring(splitIndex).trimStart();
  }

  return chunks;
}
