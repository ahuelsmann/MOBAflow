import electric from '../assets/photos/placeholder-electric.svg';
import diesel from '../assets/photos/placeholder-diesel.svg';

/**
 * The locomotive images, imported as modules so the served file names carry content hashes under
 * /assets/ (spec FR-058). They are self-drawn placeholders until the maintainer provides own
 * photographs; the example data refers to an image by these keys, never by a fixed path.
 */
export const examplePhotos: Readonly<Record<string, string>> = {
  'placeholder-diesel': diesel,
  'placeholder-electric': electric,
};
