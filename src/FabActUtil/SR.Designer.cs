//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace FabActUtil {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class SR {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal SR() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FabActUtil.SR", typeof(SR).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &apos;{0}&apos; is not a valid value for the &apos;{1}&apos; command line option.
        /// </summary>
        internal static string Error_BadArgumentValue {
            get {
                return ResourceManager.GetString("Error_BadArgumentValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Can&apos;t open command line argument file &apos;{0}&apos; : &apos;{1}&apos;.
        /// </summary>
        internal static string Error_CannotOpenCommandLineArgumentFile {
            get {
                return ResourceManager.GetString("Error_CannotOpenCommandLineArgumentFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicate &apos;{0}&apos; argument &apos;{1}&apos;.
        /// </summary>
        internal static string Error_DuplicateArgumentValue {
            get {
                return ResourceManager.GetString("Error_DuplicateArgumentValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Duplicate &apos;{0}&apos; argument.
        /// </summary>
        internal static string Error_DuplicateCommandLineArgument {
            get {
                return ResourceManager.GetString("Error_DuplicateCommandLineArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing required argument &apos;/&lt;{0}&gt;&apos;..
        /// </summary>
        internal static string Error_MissingRequiredArgument {
            get {
                return ResourceManager.GetString("Error_MissingRequiredArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing required default argument &apos;&lt;{0}&gt;&apos;..
        /// </summary>
        internal static string Error_MissingRequiredDefaultArgument {
            get {
                return ResourceManager.GetString("Error_MissingRequiredDefaultArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error: Unbalanced &apos;\&quot;&apos; in command line argument file &apos;{0}&apos;.
        /// </summary>
        internal static string Error_UnbalancedSlashes {
            get {
                return ResourceManager.GetString("Error_UnbalancedSlashes", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unrecognized command line argument &apos;{0}&apos;.
        /// </summary>
        internal static string Error_UnrecognizedCommandLineArgument {
            get {
                return ResourceManager.GetString("Error_UnrecognizedCommandLineArgument", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Failed to determine the current application, please provide application name..
        /// </summary>
        internal static string ErrorApplicationName {
            get {
                return ResourceManager.GetString("ErrorApplicationName", resourceCulture);
            }
        }
    }
}
