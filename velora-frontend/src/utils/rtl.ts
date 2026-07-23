export const isRtl = (text: string): boolean => {
  if (!text) return false;
  // Persian/Arabic Unicode range
  const rtlRegex = /[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]/;
  return rtlRegex.test(text);
};
