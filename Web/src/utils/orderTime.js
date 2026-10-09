export function utcTime(value) {
  if (!value) return NaN;
  return new Date(/[zZ]$|[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`).getTime();
}
