import br110 from '../assets/photos/br-110.webp';
import br211 from '../assets/photos/br-211.webp';

/**
 * The locomotive photos, imported as modules so the served file names carry content hashes under
 * /assets/ (spec FR-058). The example data refers to a photo by these keys, never by a fixed path.
 */
export const examplePhotos: Readonly<Record<string, string>> = {
  'br-110': br110,
  'br-211': br211,
};
