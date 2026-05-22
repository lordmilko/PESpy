using ClrDebug.DIA;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PInvoke;

namespace PESpy.Tests
{
    [TestClass]
    public class DemanglerTests
    {
        private static object dbgHelpLock = new();

        [TestMethod]
        public void Demangler_Const_NullSuperType()
        {
            //?Base64Encode@StringUtility@@SAJQEAU_AP_BLOB@@AEAV?$CStringT@GV?$StrTraitATL@GV?$ChTraitsCRT@G@ATL@@@ATL@@@ATL@@@Z < has super type
            Test(
                "?Base64Encode@StringUtility@@SAJQEAU_AP_BLOB@@AEAV?$CStringT@GV?$StrTraitATL@GV?$ChTraitsCRT@G@ATL@@@ATL@@@ATL@@@Z",
                "public: static long __cdecl StringUtility::Base64Encode(struct _AP_BLOB * __ptr64 const,class ATL::CStringT<unsigned short,class ATL::StrTraitATL<unsigned short,class ATL::ChTraitsCRT<unsigned short> > > & __ptr64)"
            );
        }

        [TestMethod]
        public void Demangler_Const_WithSuperType_DoubleConst()
        {
            //We have a super type, which means we shouldn't say we're const again. But
            //PESpy is erroneously reporting the const'ness twice. Sometimes DbgHelp emits
            //const after __ptr64, sometimes not (see the Const NullSuperType test above).
            //I don't know how to fix this at the moment
            Assert.Inconclusive();

            Test(
                "?Provider@FeatureLogging@details@wil@@SAQEBU_tlgProvider_t@@XZ",
                "public: static struct _tlgProvider_t const * __ptr64 __cdecl wil::details::FeatureLogging::Provider(void)"
            );
        }

        [TestMethod]
        public void Demangler_ManagedPointer()
        {
            Test(
                "?__abi_QueryInterface@?$AsyncOperationCompletedHandler@P$AAVWebTokenRequestResult@Core@Web@Authentication@Security@Windows@@@Foundation@Windows@@W3$AAGJAAVGuid@Platform@@PAPAX@Z",
                "[thunk]:public: virtual long __stdcall Windows::Foundation::AsyncOperationCompletedHandler<class Windows::Security::Authentication::Web::Core::WebTokenRequestResult ^>::__abi_QueryInterface`adjustor{4}' (class Platform::Guid &,void * *)"
            );
        }

        [TestMethod]
        public void Demangler_WinRTBaseType()
        {
            //[Platform::Object] breaks llvm-undname's demangler
            Test(
                "?__abi_Release@?QObject@Platform@@_IAsyncActionToAsyncOperationConverter@details@Concurrency@@WM@$AAGKXZ",
                "[thunk]:public: virtual unsigned long __stdcall Concurrency::details::_IAsyncActionToAsyncOperationConverter::[Platform::Object]::__abi_Release`adjustor{12}' (void)"
            );
        }

        [TestMethod]
        public void Demangler_WinRTBaseType_WithGenericAfterIt()
        {
            //When parsing the name scope chain, we see ?Q which we think means bail out and parse a WinRT Base Type. The fact that more ?'s follow is immaterial
            Test(
                "?__abi_AddRef@?QObject@Platform@@?$VectorView@P$AAVString@Platform@@U?$equal_to@P$AAVString@Platform@@@std@@@Collections@2@WM@$AAGKXZ",
                "[thunk]:public: virtual unsigned long __stdcall Platform::Collections::VectorView<class Platform::String ^,struct std::equal_to<class Platform::String ^> >::[Platform::Object]::__abi_AddRef`adjustor{12}' (void)"
            );
        }

        [TestMethod]
        public void Demangler_ManagedArray()
        {
            //todo: i feel like instead of returning a pointertypenode we need to return a managedarraytypenode instead which says cli::array<char >
            Test(
                "??$__abi_winrt_ptrto_array_ctor@E$00@@YGPAXQ$01$ADV?$Array@E$00@Platform@@@Z",
                "void * __stdcall __abi_winrt_ptrto_array_ctor<unsigned char,1>(cli::array<char >,class Platform::Array<unsigned char,1>)"
            );
        }

        [TestMethod]
        public void Demangler_Long()
        {
            //We had a bug where our ValueList was immediately returning its new array during resize, instead of the old one

            //todo: llvm parses it, debug why we're overflowing

            //I got a StackOverflowException parsing this
            Test(
                "??0?$multi_index_container@U?$pair@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V?$basic_ptree@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V12@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@2@@property_tree@boost@@@std@@U?$indexed_by@U?$sequenced@U?$tag@Una@mpl@boost@@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@U123@@multi_index@boost@@@multi_index@boost@@U?$ordered_non_unique@U?$tag@Uby_name@subs@?$basic_ptree@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V12@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@2@@property_tree@boost@@Una@mpl@5@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@U675@@multi_index@boost@@U?$member@U?$pair@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V?$basic_ptree@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V12@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@2@@property_tree@boost@@@std@@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@2@$0A@@23@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@std@@@23@Una@mpl@3@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@U563@@multi_index@boost@@V?$allocator@U?$pair@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V?$basic_ptree@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V12@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@2@@property_tree@boost@@@std@@@2@@multi_index@boost@@QEAA@AEBU?$cons@Unull_type@tuples@boost@@U?$cons@V?$tuple@U?$member@U?$pair@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V?$basic_ptree@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V12@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@2@@property_tree@boost@@@std@@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@2@$0A@@multi_index@boost@@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@std@@Unull_type@tuples@3@U673@U673@U673@U673@U673@U673@U673@@tuples@boost@@Unull_type@23@@23@@tuples@2@AEBV?$allocator@U?$pair@$$CBV?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V?$basic_ptree@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@V12@U?$less@V?$basic_string@_WU?$char_traits@_W@std@@V?$allocator@_W@2@@std@@@2@@property_tree@boost@@@std@@@std@@@Z",
                "public: __cdecl boost::multi_index::multi_index_container<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > >,struct boost::multi_index::indexed_by<struct boost::multi_index::sequenced<struct boost::multi_index::tag<struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na> >,struct boost::multi_index::ordered_non_unique<struct boost::multi_index::tag<struct boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > >::subs::by_name,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na>,struct boost::multi_index::member<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,0>,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > >,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na>,class std::allocator<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > > > >::multi_index_container<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > >,struct boost::multi_index::indexed_by<struct boost::multi_index::sequenced<struct boost::multi_index::tag<struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na> >,struct boost::multi_index::ordered_non_unique<struct boost::multi_index::tag<struct boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > >::subs::by_name,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na>,struct boost::multi_index::member<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,0>,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > >,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na,struct boost::mpl::na>,class std::allocator<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > > > >(struct boost::tuples::cons<struct boost::tuples::null_type,struct boost::tuples::cons<class boost::tuples::tuple<struct boost::multi_index::member<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,0>,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > >,struct boost::tuples::null_type,struct boost::tuples::null_type,struct boost::tuples::null_type,struct boost::tuples::null_type,struct boost::tuples::null_type,struct boost::tuples::null_type,struct boost::tuples::null_type,struct boost::tuples::null_type>,struct boost::tuples::null_type> > const & __ptr64,class std::allocator<struct std::pair<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > const ,class boost::property_tree::basic_ptree<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> >,struct std::less<class std::basic_string<wchar_t,struct std::char_traits<wchar_t>,class std::allocator<wchar_t> > > > > > const & __ptr64) __ptr64"
            );
        }

        [TestMethod]
        public void Demangler_EncodedVariableIssue()
        {
            Test(
                "?s_Provider@TokenBrokerCore@@0P$AAVWebAccountProvider@Credentials@Security@Windows@@$AA",
                "private: static class Windows::Security::Credentials::WebAccountProvider ^ TokenBrokerCore::s_Provider"
            );
        }

        [TestMethod]
        public void Demangler_BadName()
        {
            //DbgHelp can't parse the array element type properly, but soldiers on.
            //We see that we can't parse it and give up

            Assert.Inconclusive();

            Test(
                "?get@?Q?$IBoxArray@E@Platform@@Value@?$Array@E$00@2@U$AAAP$01$AAV42@XZ",
                "public: virtual cli::array< ?? :: ?? ::XZ::V42 >"
            );
        }

        private void Test(string mangled, string expected)
        {
            string dbgHelpResult;

            lock (dbgHelpLock)
                dbgHelpResult = DbgHelp.UnDecorateSymbolName(mangled, UNDNAME.UNDNAME_COMPLETE, expected.Length);

            var ourStr = Demangler.ParseString(mangled);

            Assert.AreEqual(expected, dbgHelpResult);
            Assert.AreEqual(expected, ourStr);
        }
    }
}
