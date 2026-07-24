// Tohumlanabilir RNG (mulberry32) — testlerde deterministiklik, oyunda Date tohumlu.
export function rngYap(tohum) {
  let a = tohum >>> 0;
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
export function aralikTam(rng, min, maxHaric) { // Unity Random.Range(int,int) karşılığı
  return min + Math.floor(rng() * (maxHaric - min));
}
