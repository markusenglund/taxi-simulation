from manim import *

class ListVariables(Scene):
    def construct(self):
      self.wait(1)
      tripValue = MathTex(r"tripValue", r"=", r"\$31.10")
      timeCost = MathTex(r"timeCost", r"=", r"\$37.20/hr")
      fare = MathTex(r"fare", r"=", r"\$12.00")
      time = MathTex(r"timeToArrival", r"=", r"21 \, min")
      time2 = MathTex(r"timeToArrival", r"=", r"0.35 \, hrs")
      # Left align the equations
      timeCost.next_to(tripValue, DOWN).align_to(tripValue, LEFT)
      fare.next_to(timeCost, DOWN).align_to(tripValue, LEFT)
      time.next_to(fare, DOWN).align_to(tripValue, LEFT)
      time2.next_to(fare, DOWN).align_to(tripValue, LEFT)
      self.play(Write(tripValue))
      self.wait(1)
      self.play(Write(timeCost))
      self.wait(1)
      self.play(Write(fare))
      self.wait(1)
      self.play(Write(time))
      self.wait(1)
      self.play(ReplacementTransform(time, time2))
      self.wait(3)


class NetValueCalculation(Scene):
    def construct(self):
        equationStart = MathTex(r"netValue", r"=", r"tripValue", r"- fare", r"- (timeCost \cdot timeToArrival)")
        equationStart[0].set_color(YELLOW)
        equationWithValues = MathTex(r"=", r"\$31.10", r"- \$12.00", r"- (\$37.20/hr \cdot 0.35 \, hrs)")
        equationCombined = MathTex(r"=", r"\$31.10", r"- \$12.00", r"- \$13.02")
        equationSimplified = MathTex(r"=", r"\$6.08")

        equationWithValues.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationCombined.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationSimplified.next_to(equationWithValues, DOWN).align_to(equationStart[1], LEFT)

        self.play(Write(equationStart))
        self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[1], equationWithValues[0]),
            TransformFromCopy(equationStart[2], equationWithValues[1]),
            TransformFromCopy(equationStart[3], equationWithValues[2]),
            TransformFromCopy(equationStart[4], equationWithValues[3]),
            # TransformFromCopy(equationStart[5], equationWithValues[4]),
            # TransformFromCopy(equationStart[6], equationWithValues[5]),
            # TransformFromCopy(equationStart[7], equationWithValues[6]),
            # TransformFromCopy(equationStart[8], equationWithValues[7]),
            # TransformFromCopy(equationStart[9], equationWithValues[8]),
            # TransformFromCopy(equationStart[10], equationWithValues[9]),
            lag_ratio=0.6,
            run_time=3
        ))
        self.wait(1)
        self.play(Transform(equationWithValues, equationCombined))
        self.wait(1)
        self.play(TransformFromCopy(equationCombined, equationSimplified))

        self.wait(3)