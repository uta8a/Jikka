module Jikka.Core.Convert.MakeEagerSpec (spec) where

import Jikka.Core.Convert.MakeEager (run)
import Jikka.Core.Language.Expr
import Test.Hspec

spec :: Spec
spec = describe "run" $ do
  it "works" $ do
    let prog =
          ResultExpr
            ( AppBuiltin
                (If IntTy)
                [ LitTrue,
                  Lit0,
                  Lit1
                ]
            )
    let expected =
          ResultExpr
            ( App
                ( AppBuiltin
                    (If (FunTy [] IntTy))
                    [ LitTrue,
                      Lam [] Lit0,
                      Lam [] Lit1
                    ]
                )
                []
            )
    run prog `shouldBe` Right expected
