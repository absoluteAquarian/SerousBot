namespace SerousBot.DataStructures {
	public class PasteContents {
		public readonly string filename;
		public readonly string contents;
		public readonly bool successful;

		public static PasteContents Failed => new(null, null, false);

		public PasteContents(string filename, string contents, bool successful) {
			this.filename = filename;
			this.contents = contents;
			this.successful = successful;
		}
	}
}
