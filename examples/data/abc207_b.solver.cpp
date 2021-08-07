#include <bits/stdc++.h>
#define REP(i, n) for (int i = 0; (i) < (int)(n); ++(i))
#define REP3(i, m, n) for (int i = (m); (i) < (int)(n); ++(i))
#define REP_R(i, n) for (int i = (int)(n)-1; (i) >= 0; --(i))
#define REP3R(i, m, n) for (int i = (int)(n)-1; (i) >= (int)(m); --(i))
#define ALL(x) ::std::begin(x), ::std::end(x)
using namespace std;

int64_t solve(int64_t A, int64_t B, int64_t C, int64_t D) {
    int64_t ans = -1;
    if (D*C - B > 0){
        ans = (A + D * C - B - 1) / (D*C - B);
    }
    return ans;
}

// generated by oj-template v4.8.1 (https://github.com/online-judge-tools/template-generator)
int main() {
    std::ios::sync_with_stdio(false);
    std::cin.tie(nullptr);
    int64_t A, B, C, D;
    std::cin >> A >> B >> C >> D;
    auto ans = solve(A, B, C, D);
    std::cout << ans << '\n';
    return 0;
}
